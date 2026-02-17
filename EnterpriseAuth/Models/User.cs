namespace EnterpriseAuth.Models;

public class User
{
    public Guid Id { get; set; }

    public string Username { get; set; }

    public List<UserKey> Keys { get; set; } = new();
}
