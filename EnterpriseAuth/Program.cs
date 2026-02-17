using EnterpriseAuth.Data;
using EnterpriseAuth.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using NSec.Cryptography;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

var jwtKey = builder.Configuration["JwtKey"]
    ?? "SUPER_SECRET_KEY_CHANGE_IN_PRODUCTION_123456789";


// Database (SQLite default)
builder.Services.AddDbContext<AuthDbContext>(options =>
    options.UseSqlite("Data Source=auth.db"));


// PostgreSQL version (uncomment for production)
// builder.Services.AddDbContext<AuthDbContext>(options =>
//     options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));


// Authentication
builder.Services
.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
.AddJwtBearer(options =>
{
    options.TokenValidationParameters =
        new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,

            IssuerSigningKey =
                new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwtKey))
        };
});

builder.Services.AddAuthorization();


// Rate Limiting
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("auth", config =>
    {
        config.PermitLimit = 5;
        config.Window = TimeSpan.FromMinutes(1);
        config.QueueLimit = 0;
    });
});

var app = builder.Build();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();


// Health check
app.MapGet("/", () => "Auth Server Running");


// Register User
app.MapPost("/register-user", async (
    string username,
    AuthDbContext db) =>
{
    if (await db.Users.AnyAsync(x => x.Username == username))
        return Results.BadRequest("User exists");

    var user = new User
    {
        Id = Guid.NewGuid(),
        Username = username
    };

    db.Users.Add(user);
    await db.SaveChangesAsync();

    return Results.Ok(user);
});


// Register Public Key
app.MapPost("/register-key", async (
    RegisterKeyRequest request,
    AuthDbContext db) =>
{
    var user = await db.Users.FindAsync(request.UserId);

    if (user == null)
        return Results.NotFound();

    var key = new UserKey
    {
        Id = Guid.NewGuid(),
        UserId = request.UserId,
        PublicKey =
            Convert.FromBase64String(request.PublicKeyBase64),
        CreatedAt = DateTime.UtcNow,
        IsRevoked = false
    };

    db.Keys.Add(key);

    await db.SaveChangesAsync();

    return Results.Ok("Key registered");
});


// Request Challenge
app.MapPost("/auth/request", async (
    string username,
    AuthDbContext db,
    HttpContext http) =>
{
    var user =
        await db.Users.FirstOrDefaultAsync(
            u => u.Username == username);

    if (user == null)
        return Results.NotFound();

    var challengeBytes =
        RandomNumberGenerator.GetBytes(32);

    var challenge = new Challenge
    {
        Id = Guid.NewGuid(),
        UserId = user.Id,
        Value = challengeBytes,
        ExpiresAt = DateTime.UtcNow.AddMinutes(5)
    };

    db.Challenges.Add(challenge);

    await db.SaveChangesAsync();

    return Results.Ok(new
    {
        challengeId = challenge.Id,
        challenge =
            Convert.ToBase64String(challengeBytes)
    });
})
.RequireRateLimiting("auth");


// Verify Login
app.MapPost("/auth/verify", async (
    VerifyRequest request,
    AuthDbContext db,
    HttpContext http) =>
{
    var challenge =
        await db.Challenges.FindAsync(
            request.ChallengeId);

    if (challenge == null ||
        challenge.ExpiresAt < DateTime.UtcNow)
        return Results.Unauthorized();

    var keys =
        await db.Keys
        .Where(k =>
            k.UserId == challenge.UserId &&
            !k.IsRevoked)
        .ToListAsync();

    var signature =
        Convert.FromBase64String(
            request.SignatureBase64);

    bool valid = false;

    foreach (var key in keys)
    {
        var pubKey =
            PublicKey.Import(
                SignatureAlgorithm.Ed25519,
                key.PublicKey,
                KeyBlobFormat.RawPublicKey);

        valid =
            SignatureAlgorithm.Ed25519.Verify(
                pubKey,
                challenge.Value,
                signature);

        if (valid)
            break;
    }

    if (!valid)
        return Results.Unauthorized();

    var jwt =
        GenerateJwt(jwtKey, challenge.UserId);

    var refreshToken =
        Guid.NewGuid().ToString();

    db.RefreshTokens.Add(
        new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = challenge.UserId,
            Token = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddDays(30),
            Revoked = false
        });

    db.AuditLogs.Add(
        new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = challenge.UserId,
            Action = "LOGIN",
            CreatedAt = DateTime.UtcNow,
            IpAddress =
                http.Connection.RemoteIpAddress?.ToString()
                ?? ""
        });

    await db.SaveChangesAsync();

    return Results.Ok(new
    {
        token = jwt,
        refreshToken
    });
});


// Refresh JWT
app.MapPost("/auth/refresh", async (
    string refreshToken,
    AuthDbContext db) =>
{
    var token =
        await db.RefreshTokens
        .FirstOrDefaultAsync(
            t => t.Token == refreshToken &&
                 !t.Revoked);

    if (token == null)
        return Results.Unauthorized();

    var jwt =
        GenerateJwt(jwtKey, token.UserId);

    return Results.Ok(new { token = jwt });
});


// Logout
app.MapPost("/auth/logout", async (
    ClaimsPrincipal user,
    AuthDbContext db) =>
{
    var userId =
        Guid.Parse(
            user.FindFirst("sub")!.Value);

    var tokens =
        await db.RefreshTokens
        .Where(t => t.UserId == userId)
        .ToListAsync();

    foreach (var token in tokens)
    {
        token.Revoked = true;
        token.RevokedAt = DateTime.UtcNow;
    }

    await db.SaveChangesAsync();

    return Results.Ok("Logged out");
})
.RequireAuthorization();


// Revoke Refresh Token
app.MapPost("/auth/revoke-refresh", async (
    string refreshToken,
    AuthDbContext db) =>
{
    var token =
        await db.RefreshTokens
        .FirstOrDefaultAsync(
            t => t.Token == refreshToken);

    if (token == null)
        return Results.NotFound();

    token.Revoked = true;
    token.RevokedAt = DateTime.UtcNow;

    await db.SaveChangesAsync();

    return Results.Ok();
});


// Add Key (Key Rotation)
app.MapPost("/auth/add-key", async (
    ClaimsPrincipal user,
    string publicKeyBase64,
    AuthDbContext db) =>
{
    var userId =
        Guid.Parse(
            user.FindFirst("sub")!.Value);

    db.Keys.Add(
        new UserKey
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            PublicKey =
                Convert.FromBase64String(publicKeyBase64),
            CreatedAt = DateTime.UtcNow
        });

    await db.SaveChangesAsync();

    return Results.Ok();
})
.RequireAuthorization();


// Protected Endpoint
app.MapGet("/secure", () =>
{
    return "Authenticated OK";
})
.RequireAuthorization();


app.Run();


// JWT Generator
static string GenerateJwt(
    string jwtKey,
    Guid userId)
{
    var key =
        new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtKey));

    var creds =
        new SigningCredentials(
            key,
            SecurityAlgorithms.HmacSha256);

    var claims = new[]
    {
        new Claim("sub", userId.ToString())
    };

    var token =
        new JwtSecurityToken(
            expires: DateTime.UtcNow.AddMinutes(15),
            signingCredentials: creds,
            claims: claims);

    return new JwtSecurityTokenHandler()
        .WriteToken(token);
}


// Request Models
public class RegisterKeyRequest
{
    public Guid UserId { get; set; }
    public string PublicKeyBase64 { get; set; } = "";
}

public class VerifyRequest
{
    public Guid ChallengeId { get; set; }
    public string SignatureBase64 { get; set; } = "";
}
