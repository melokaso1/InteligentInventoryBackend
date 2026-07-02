namespace Domain.Entities;



public class User

{

    public Guid Id { get; set; }

    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public int RoleId { get; set; }

    public Role Role { get; set; } = null!;

    public Guid? CustomerId { get; set; }

    public Customer? Customer { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

}

