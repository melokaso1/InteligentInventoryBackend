using System.Text;
using Application.Abstractions;
using Application.Models;
using Domain.Entities;
using Domain.Enums;

namespace Application.Services;

public sealed class InvoiceService(
    IInvoiceRepository invoiceRepository,
    ICustomerRepository customerRepository,
    IUnitOfWork unitOfWork) : IInvoiceService
{
    private const decimal TaxRate = 0.08m;

    public async Task<PagedResult<Invoice>> GetInvoicesAsync(InvoiceQueryModel query, CancellationToken cancellationToken = default)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 200);
        var status = ParseInvoiceStatus(query.Status);

        return await invoiceRepository.GetPagedAsync(page, pageSize, status, cancellationToken);
    }

    public async Task<InvoiceStatsModel> GetStatsAsync(CancellationToken cancellationToken = default)
    {
        return new InvoiceStatsModel
        {
            TotalInvoices = await invoiceRepository.CountAsync(cancellationToken),
            PaidInvoices = await invoiceRepository.CountByStatusAsync(InvoiceStatus.Paid, cancellationToken),
            PendingInvoices = await invoiceRepository.CountByStatusAsync(InvoiceStatus.Pending, cancellationToken),
            OverdueInvoices = await invoiceRepository.CountByStatusAsync(InvoiceStatus.Overdue, cancellationToken),
            DraftInvoices = await invoiceRepository.CountByStatusAsync(InvoiceStatus.Draft, cancellationToken),
            TotalBilledAmount = await invoiceRepository.SumTotalAsync(cancellationToken),
        };
    }

    public Task<Invoice?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        invoiceRepository.GetByIdWithLineItemsAsync(id, cancellationToken);

    public async Task<string> BuildPdfContentAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var invoice = await invoiceRepository.GetByIdWithLineItemsAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException("Factura no encontrada.");

        var content = new StringBuilder();
        content.AppendLine($"Factura: {invoice.InvoiceNumber}");
        content.AppendLine($"Cliente: {invoice.ClientName}");
        content.AppendLine($"Fecha: {invoice.IssueDate:yyyy-MM-dd}");
        content.AppendLine($"Vencimiento: {invoice.DueDate:yyyy-MM-dd}");
        content.AppendLine($"Estado: {ToFrontendInvoiceStatus(invoice.Status)}");
        content.AppendLine($"Subtotal: COP {invoice.Subtotal:0.00}");
        content.AppendLine($"IVA: COP {invoice.Tax:0.00}");
        content.AppendLine($"Total: COP {invoice.Total:0.00}");
        content.AppendLine();
        content.AppendLine("Items:");

        foreach (var item in invoice.LineItems)
        {
            content.AppendLine($"- {item.Description} x{item.Quantity} @ COP {item.UnitPrice:0.00}");
        }

        return content.ToString();
    }

    public async Task<Invoice> CreateAsync(CreateInvoiceModel request, CancellationToken cancellationToken = default)
    {
        var clientName = request.Client.Trim();
        if (string.IsNullOrWhiteSpace(clientName))
        {
            throw new InvalidOperationException("El nombre del cliente es obligatorio.");
        }

        if (request.LineItems.Count == 0)
        {
            throw new InvalidOperationException("Debe enviar al menos una línea en la factura.");
        }

        foreach (var line in request.LineItems)
        {
            if (string.IsNullOrWhiteSpace(line.Description))
            {
                throw new InvalidOperationException("Cada línea debe tener una descripción.");
            }

            if (line.Quantity <= 0)
            {
                throw new InvalidOperationException("La cantidad de cada línea debe ser mayor a cero.");
            }

            if (line.UnitPrice < 0)
            {
                throw new InvalidOperationException("El precio unitario no puede ser negativo.");
            }
        }

        if (!TryParseInvoiceDate(request.Date, out var issueDate))
        {
            throw new InvalidOperationException("Fecha de emisión inválida.");
        }

        if (!TryParseInvoiceDate(request.DueDate, out var dueDate))
        {
            throw new InvalidOperationException("Fecha de vencimiento inválida.");
        }

        var subtotal = decimal.Round(
            request.LineItems.Sum(li => li.UnitPrice * li.Quantity),
            2,
            MidpointRounding.AwayFromZero);
        var tax = decimal.Round(subtotal * TaxRate, 2, MidpointRounding.AwayFromZero);
        var total = subtotal + tax;

        if (subtotal < 0 || tax < 0 || total < 0)
        {
            throw new InvalidOperationException("Los totales de la factura no pueden ser negativos.");
        }

        var placeholderEmail = $"standalone-{Guid.NewGuid():N}@elplonsazo.local";
        var customer = await customerRepository.GetOrCreateAsync(clientName, placeholderEmail, cancellationToken);

        var now = DateTime.UtcNow;
        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            InvoiceNumber = $"INV-{now:yyyyMMddHHmmssfff}",
            CustomerId = customer.Id,
            ClientName = clientName,
            ClientInitials = BuildInitials(clientName),
            BillingNote = request.BillingNote.Trim(),
            Status = InvoiceStatus.Draft,
            Subtotal = subtotal,
            Tax = tax,
            Total = total,
            IssueDate = issueDate,
            DueDate = dueDate,
            LineItems = request.LineItems
                .Select(li => new InvoiceLineItem
                {
                    Id = Guid.NewGuid(),
                    Description = li.Description.Trim(),
                    Quantity = li.Quantity,
                    UnitPrice = li.UnitPrice,
                })
                .ToList(),
        };

        invoiceRepository.Add(invoice);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return invoice;
    }

    private static bool TryParseInvoiceDate(string value, out DateTime date)
    {
        if (DateTime.TryParse(value, out date))
        {
            date = DateTime.SpecifyKind(date.Date, DateTimeKind.Utc);
            return true;
        }

        date = default;
        return false;
    }

    private static InvoiceStatus? ParseInvoiceStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return null;
        }

        return status.Trim().ToLowerInvariant() switch
        {
            "paid" => InvoiceStatus.Paid,
            "pending" => InvoiceStatus.Pending,
            "overdue" => InvoiceStatus.Overdue,
            "draft" => InvoiceStatus.Draft,
            _ => throw new InvalidOperationException("Estado inválido. Valores permitidos: paid, pending, overdue, draft."),
        };
    }

    private static string ToFrontendInvoiceStatus(InvoiceStatus status) => status switch
    {
        InvoiceStatus.Paid => "paid",
        InvoiceStatus.Pending => "pending",
        InvoiceStatus.Overdue => "overdue",
        InvoiceStatus.Draft => "draft",
        _ => "draft",
    };

    private static string BuildInitials(string input)
    {
        var initials = input
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(part => part.Length > 0)
            .Take(2)
            .Select(part => char.ToUpperInvariant(part[0]))
            .ToArray();

        return initials.Length == 0 ? "NN" : new string(initials);
    }
}
