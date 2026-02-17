# EnterpriseAuth 🔐

Enterprise-grade passwordless authentication system for ASP.NET Core using Ed25519 public/private key cryptography, SSH-style challenge-response authentication, and JWT authorization.

Eliminates passwords entirely and provides modern, secure, scalable authentication.

---

# ✨ Features

- Passwordless authentication
- SSH-style challenge-response login
- Ed25519 asymmetric cryptography
- JWT authentication
- Refresh token support
- Logout and token revocation
- Key rotation
- Audit logging
- Rate limiting protection
- Stateless authentication
- Production-ready architecture
- PostgreSQL support

---

# 🧠 How It Works

Authentication flow:

Client Server

Request challenge →
← challenge

Sign challenge →

Send signature →
← JWT + Refresh Token


Private keys never leave the client.

Server stores only public keys.

---

# 🏗 Architecture

/EnterpriseAuth
│
├ EnterpriseAuth/
│ ├ Program.cs
│ ├ Data/
│ │ └ AuthDbContext.cs
│ ├ Models/
│ │ ├ User.cs
│ │ ├ UserKey.cs
│ │ ├ Challenge.cs
│ │ ├ RefreshToken.cs
│ │ └ AuditLog.cs
│ └ EnterpriseAuth.csproj
│
├ KeyGen/
│ └ Program.cs
│
├ README.md
└ EnterpriseAuth_Full_Documentation.pdf


---

# 🔐 Security Model

EnterpriseAuth uses:

- Ed25519 asymmetric cryptography
- Challenge-response authentication
- JWT tokens
- Refresh token rotation
- Key revocation
- Audit logging
- Rate limiting

Private key never leaves the client.

Prevents:

- password leaks
- brute force attacks
- credential stuffing
- phishing attacks
- replay attacks

---

# ⚙️ Requirements

- .NET 8 SDK
- SQLite (default)
- PostgreSQL (production recommended)

Install SDK:

https://dotnet.microsoft.com/download

---

# ▶️ Running the Server

dotnet restore
dotnet build
dotnet run

Server starts at:
http://localhost:5266/


---

# 🔑 Generate Key Pair

Run KeyGen client:

dotnet run


Creates:

private.key
public.key


---

# 👤 Register User

curl -X POST "http://localhost:5266/register-user?username=test

---

# 🔐 Register Public Key

curl -X POST http://localhost:5266/register-key-H "Content-Type: application/json"-d '{"userId":"USER_ID","publicKeyBase64":"PUBLIC_KEY"}'


---

# 🚀 Login

Client signs challenge automatically:

dotnet run

Response:

{
"token": "JWT_TOKEN",
"refreshToken": "REFRESH_TOKEN"
}

---

# 🛡 Access Secure Endpoint

curl http://localhost:5266/secure-H "Authorization: Bearer JWT_TOKEN"


---

# 🔄 Refresh Token

curl -X POST"http://localhost:5266/auth/refresh?refreshToken=REFRESH_TOKEN


---

# 🚪 Logout

curl -X POST http://localhost:5266/auth/logout-H "Authorization: Bearer JWT_TOKEN"

---

# 🔁 Key Rotation

curl -X POST http://localhost:5266/auth/add-key-H "Authorization: Bearer JWT_TOKEN"-d "publicKeyBase64=NEW_KEY"


---

# 📡 API Endpoints

| Endpoint | Method | Description |
|--------|--------|-------------|
| /register-user | POST | Register new user |
| /register-key | POST | Register public key |
| /auth/request | POST | Request challenge |
| /auth/verify | POST | Verify login |
| /auth/refresh | POST | Refresh JWT |
| /auth/logout | POST | Logout |
| /auth/add-key | POST | Add new key |
| /secure | GET | Protected endpoint |

---

# 🧾 Database Schema

Tables:

- Users
- UserKeys
- Challenges
- RefreshTokens
- AuditLogs

---

# 🐘 PostgreSQL Setup (Production)

Install provider:

dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL
Update Program.cs:
options.UseNpgsql(connectionString);


---

# 🧪 Rate Limiting

Default:

5 authentication attempts per minute


Prevents brute force attacks.

---

# 🔐 Cryptography

Algorithm:

Ed25519


Advantages:

- secure
- fast
- modern
- widely used

Used by:

- SSH
- GitHub
- Cloudflare
- Google

---

# 🚀 Production Deployment

Recommended stack:

- ASP.NET Core
- PostgreSQL
- Docker
- HTTPS
- nginx reverse proxy

---

# 📈 Scalability

Supports:

- millions of users
- horizontal scaling
- stateless authentication

---

# 🧾 Documentation

Full documentation available:

EnterpriseAuth_Full_Documentation.pdf


---

# 🧑‍💻 Author

Seyed Ali Nazeri

GitHub:
https://github.com/seyed-ali-nazeri

---

# 📄 License

MIT License

---

# ⭐ Summary

EnterpriseAuth is a modern, secure, passwordless authentication system designed for enterprise applications using SSH-style cryptography and JWT authorization.




