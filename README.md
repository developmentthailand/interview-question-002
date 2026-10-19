# Interview Question 02 — Registration and JWT Login

A responsive account flow based on the No2 exam reference screens: register, sign in, then display the welcome screen only after the API validates the signed access token.

## Features

- Username registration with Unicode normalization and a database-enforced unique index.
- Password confirmation and a 15–128 Unicode-code-point password length rule.
- ASP.NET Core Identity password hashing; only the hash is persisted.
- Generic login errors, five login attempts per client IP per minute, and a `Retry-After` header for throttled requests.
- Signed 15-minute JWTs with issuer, audience, signature, and lifetime validation.
- Protected `GET /api/auth/me`; Angular keeps the access token in memory and shows the welcome screen only after this endpoint succeeds.
- SQLite database upgraded with EF Core migrations. No default accounts are created.
- Accessible labels, keyboard focus styles, pending/error feedback, password-manager support, and a responsive layout.

## Requirements

- .NET 10 SDK
- Node.js 22.22.3 or newer and npm 10.9.8 or newer

## Configure the local signing key

The API refuses to start unless it has an issuer, audience, and a base64 signing key containing at least 32 random bytes. Store these in .NET User Secrets for local development:

```powershell
dotnet user-secrets set "Jwt:Issuer" "https://interview-question-002.example.com" --project backend/Auth.Api/Auth.Api.csproj
dotnet user-secrets set "Jwt:Audience" "interview-question-002.example.com" --project backend/Auth.Api/Auth.Api.csproj
$signingKey = [Convert]::ToBase64String([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(32))
dotnet user-secrets set "Jwt:SigningKey" $signingKey --project backend/Auth.Api/Auth.Api.csproj
Remove-Variable signingKey
```

Never commit the generated signing key. Deployment should supply the same settings through a managed secret provider and use HTTPS.

## Run locally

Start the API in one terminal:

```powershell
dotnet run --project backend/Auth.Api/Auth.Api.csproj --launch-profile http
```

The API listens on `http://localhost:5081`. Start Angular in another terminal:

```powershell
cd frontend
npm ci
npm start
```

Open `http://localhost:4201`. The Angular development proxy forwards `/api` to port `5081`. The SQLite file is created at `backend/Auth.Api/auth.db` and is excluded from source control.

## API contract

| Method | Path | Request / response | Result |
| --- | --- | --- | --- |
| `POST` | `/api/auth/register` | Request: `{ "username", "password", "confirmPassword" }`; response: `{ "id", "username" }` | `201`, `400` validation problem, or `409` duplicate username |
| `POST` | `/api/auth/login` | Request: `{ "username", "password" }`; response: `{ "accessToken", "tokenType", "expiresAtUtc" }` | `200`, generic `401`, or rate-limit `429` |
| `GET` | `/api/auth/me` | Bearer token; response: `{ "id", "username" }` | `200` or `401` |

Usernames are trimmed, normalized with Unicode Form KC and invariant uppercase, and limited to 64 Unicode code points. Passwords are not trimmed or normalized; spaces and Unicode are accepted, confirmation is an exact match, and no character-class rule is imposed.

## Verification

Run from `interview-question-002/`:

```powershell
dotnet test tests/backend/Auth.Api.Tests/Auth.Api.Tests.csproj --configuration Release
dotnet build backend/Auth.Api/Auth.Api.csproj --configuration Release
cd frontend
npm test
npm run build
```

Verified on 20 October 2026:

- Backend integration tests: **23 passed, 0 failed**. Coverage includes hashing and response leakage, username and password boundaries, Unicode normalization, duplicate names, generic login failures, JWT validation and configuration, password rehash, and rate limiting.
- Backend Release build: **0 warnings, 0 errors**.
- Angular tests: **8 passed, 0 failed**. Coverage includes screen transitions, masked fields, password mismatch, duplicate submission protection, API requests, Bearer attachment, login errors, and withholding Welcome when token validation fails.
- Angular production build: **219.97 kB initial bundle** (estimated transfer size **59.23 kB**); build completed successfully.
- Browser flow: registration, login, and the protected welcome screen were exercised through `http://localhost:4201` against the API on port `5081`. The temporary QA account is removed after verification; no default account is seeded.

## Exam naming and submission note

The project uses `Example.Com.InterviewQuestion002`, npm package `@example-com/interview-question-002`, and the example identity `interview-question-002.example.com`. The attached exam guide specifies GitLab while the user-provided workflow mentions GitHub. No remote repository or email submission is configured until the destination is clear.
