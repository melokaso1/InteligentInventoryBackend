using System.Text;
using Application.Abstractions;
using Application.Models;
using Domain.Entities;
using Domain.Enums;

namespace Application.Services;

public sealed class InvoiceService(IInvoiceRepository invoiceRepository) : IInvoiceService
{
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
}
