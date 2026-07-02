using Domain.Entities;

namespace Application.Models;

public sealed class ChatbotSaleResult
{
    public required Sale Sale { get; init; }
    public required string InvoiceNumber { get; init; }
}
