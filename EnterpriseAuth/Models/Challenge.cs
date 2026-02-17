namespace EnterpriseAuth.Models;

public class Challenge
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public byte[] Value { get; set; }

    public DateTime ExpiresAt { get; set; }
}
