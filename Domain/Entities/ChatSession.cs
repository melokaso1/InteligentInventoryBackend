using Domain.Enums;

namespace Domain.Entities;

public class ChatSession
{
    public Guid Id { get; set; }
    public string SessionToken { get; set; } = string.Empty;
    public Guid? CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public Guid? UserId { get; set; }
    public User? User { get; set; }
    public string? CurrentStateJson { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<ChatMessage> Messages { get; set; } = [];
    public ICollection<Sale> Sales { get; set; } = [];
}
