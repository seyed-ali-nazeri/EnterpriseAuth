namespace EnterpriseAuth.Models;

public class AuditLog
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string Action { get; set; } = "";

    public DateTime CreatedAt { get; set; }

    public string IpAddress { get; set; } = "";


}
