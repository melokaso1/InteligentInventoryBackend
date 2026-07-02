using Domain.Enums;

namespace Domain.Entities;

public class ChatMessage
{
    public Guid Id { get; set; }
    public Guid ChatSessionId { get; set; }
    public ChatSession ChatSession { get; set; } = null!;
    public ChatSenderType SenderType { get; set; }
    public string MessageText { get; set; } = string.Empty;
    public string? MetadataJson { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
