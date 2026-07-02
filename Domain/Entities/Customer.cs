namespace Domain.Entities;

public class Customer
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? DocumentNumber { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User? User { get; set; }
    public ICollection<Sale> Sales { get; set; } = [];
    public ICollection<Invoice> Invoices { get; set; } = [];
    public ICollection<ChatSession> ChatSessions { get; set; } = [];
}
