namespace EnterpriseAuth.Models;

public class UserKey
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public User User { get; set; }

    public byte[] PublicKey { get; set; }

    public DateTime CreatedAt { get; set; }

    public bool IsRevoked { get; set; }
}
