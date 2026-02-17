using Microsoft.EntityFrameworkCore;
using EnterpriseAuth.Models;

namespace EnterpriseAuth.Data;

public class AuthDbContext : DbContext
{
    public AuthDbContext(DbContextOptions<AuthDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();

    public DbSet<UserKey> Keys => Set<UserKey>();

    public DbSet<Challenge> Challenges => Set<Challenge>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

}
