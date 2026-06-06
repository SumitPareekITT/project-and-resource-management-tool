# Day 3 Code Explanation

This file explains what was implemented on Day 3 and why it was needed for the PRM project.

Day 3 goal: implement the authentication foundation from the BRD:

- Password hashing.
- Login service logic.
- Forced password change.
- Auth API endpoints.
- Tests for the main authentication rules.

## Why Day 3 Matters

The BRD says every user must log in before accessing Admin, Manager, or Employee menus.

It also says:

- Admin creates user accounts.
- First admin is seeded directly.
- Admin-created users must change password on first login.
- Inactive users cannot log in.
- Login failure must return an error.

Day 3 implements the server-side foundation for those rules.

## Shared Auth Contracts

Path: `src/Shared/DTOs/Auth`

These files define the request/response objects used between the console client and the server.

## LoginRequest

```csharp
public sealed record LoginRequest(string Username, string Password);
```

Why:

- The console client sends username/password to the server.
- This avoids sending raw entity objects over the API.

## LoginResponse

```csharp
public sealed record LoginResponse(
    int UserId,
    string FullName,
    string Username,
    UserRole Role,
    bool ForcePasswordChange);
```

Why:

- The client needs user identity and role to open the correct menu.
- `ForcePasswordChange` tells the client whether to show the change-password screen before showing any role menu.

## ChangePasswordRequest

```csharp
public sealed record ChangePasswordRequest(
    int UserId,
    string NewPassword,
    string ConfirmPassword);
```

Why:

- The BRD requires a first-login password change flow.
- The server must validate both password fields.

## ChangePasswordResponse

```csharp
public sealed record ChangePasswordResponse(int UserId, bool ForcePasswordChange);
```

Why:

- The client needs to know the password-change requirement is cleared.

## AuthResult and AuthResultCode

Files:

- `AuthResult.cs`
- `AuthResultCode.cs`

`AuthResult<T>` wraps service results in a consistent shape:

- success/failure
- result code
- message
- optional value

`AuthResultCode` contains fixed outcomes:

- `Success`
- `InvalidCredentials`
- `InactiveUser`
- `UserNotFound`
- `PasswordTooShort`
- `PasswordMismatch`

Why:

- Services should not directly return HTTP responses.
- Controllers can translate these result codes to correct HTTP status codes.
- Tests can assert exact business outcomes without depending on controller formatting.

## Password Hashing

Files:

- `src/Server/Services/IPasswordHasher.cs`
- `src/Server/Services/Pbkdf2PasswordHasher.cs`

## IPasswordHasher

```csharp
public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string passwordHash);
}
```

Why:

- AuthService depends on an abstraction, not a concrete hashing implementation.
- This supports the Dependency Inversion Principle.
- Tests and future changes are easier.

## Pbkdf2PasswordHasher

This class uses built-in .NET cryptography:

- `RandomNumberGenerator.GetBytes` for salt.
- `Rfc2898DeriveBytes.Pbkdf2` for key derivation.
- SHA-256.
- 100,000 iterations.
- `CryptographicOperations.FixedTimeEquals` for comparison.

Hash format:

```text
PBKDF2.{iterations}.{base64Salt}.{base64Key}
```

Why:

- Passwords should never be stored as plain text.
- PBKDF2 is a proven password hashing approach available in .NET.
- Each new password hash gets a random salt.
- Fixed-time comparison reduces timing attack risk.

## Seed Admin Password

Path: `src/Server/Data/SeedData.cs`

The first admin user now has a real hash for the BRD default password:

```text
admin / Admin@1234
```

Why:

- The BRD requires first admin bootstrap.
- Login must work before Admin can create more users.
- The seeded admin still has `ForcePasswordChange = true`, so the user must change password on first login.

Note:

The seed hash uses the same PBKDF2 format. It is deterministic only because seed data must be fixed in source code.

## AuthService

Path: `src/Server/Services/AuthService.cs`

This is the business logic for authentication.

It depends on:

- `UserRepository`
- `IPasswordHasher`

Why:

- Repositories handle database access.
- Password hasher handles cryptography.
- AuthService focuses on auth business rules.

## LoginAsync

Main steps:

1. Validate username/password were provided.
2. Load the user by username.
3. Verify the supplied password against the stored hash.
4. Block inactive users.
5. Update `LastLoginAtUtc`.
6. Return user identity, role, and force-password-change flag.

Why:

- Implements BRD login behavior.
- Keeps role-menu decision data in the response.
- Ensures inactive users cannot access the system.

Important behavior:

- Unknown user and wrong password both return `InvalidCredentials`.

Why:

- This avoids revealing whether a username exists.

## ChangePasswordAsync

Main steps:

1. Validate minimum password length.
2. Validate new password and confirm password match.
3. Load user by ID.
4. Block inactive users.
5. Hash the new password.
6. Set `ForcePasswordChange = false`.
7. Save changes.

Why:

- Implements the BRD forced-password-change screen.
- Once password changes successfully, the user can continue to their menu.

## AuthController

Path: `src/Server/Controllers/AuthController.cs`

Endpoints:

```text
POST /api/auth/login
POST /api/auth/change-password
```

Why:

- The console client needs REST APIs for login and password change.
- The BRD requires client-server behavior, not local-only login.

## Controller Result Mapping

`AuthController` maps service result codes to HTTP status codes:

- Success -> `200 OK`
- Invalid credentials -> `401 Unauthorized`
- Inactive user -> `403 Forbidden`
- User not found -> `404 Not Found`
- Password validation errors -> `400 Bad Request`

Why:

- Services stay independent from HTTP.
- Controllers handle API-specific response codes.

## Program.cs Registration

Path: `src/Server/Program.cs`

Added registrations:

```csharp
builder.Services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
builder.Services.AddScoped<AuthService>();
```

Why:

- Dependency injection creates and supplies dependencies automatically.
- Password hasher has no per-request state, so singleton is fine.
- AuthService uses DbContext/repositories, so it is scoped per request.

## Tests

Path: `tests/Server.Tests/AuthServiceTests.cs`

Day 3 added tests for the auth service.

## Test Package

Added:

```xml
Microsoft.EntityFrameworkCore.InMemory
```

Why:

- AuthService needs repository/database behavior.
- InMemory lets tests run without MySQL.

## Test Coverage Added

### Valid Login

Verifies:

- Login succeeds with correct username/password.
- Response includes username, role, and force-password-change flag.

Why:

- This is the happy path for all users.

### Wrong Password

Verifies:

- Login fails.
- Result code is `InvalidCredentials`.

Why:

- Required login failure behavior.

### Inactive User

Verifies:

- Inactive users cannot log in.
- Result code is `InactiveUser`.

Why:

- BRD says deactivated users are blocked.

### Valid Password Change

Verifies:

- Password change succeeds.
- New hash verifies with the new password.
- `ForcePasswordChange` becomes false.

Why:

- Required first-login behavior.

### Password Mismatch

Verifies:

- Password change fails if confirmation does not match.

Why:

- Required validation for change-password screen.

### Password Hasher

Verifies:

- Correct password validates.
- Wrong password fails.

Why:

- Protects the most security-sensitive helper.

## What Day 3 Does Not Do Yet

Day 3 does not implement a full authorization/session system yet.

Not done:

- JWT or token-based authorization.
- Role attributes on controllers.
- Client-side session manager integration.
- Admin create-user API.
- Password reset API.
- User deactivation API.

These belong to the remaining Day 3/Day 4 user-management work.

## How This Supports The BRD

BRD requirement: Login for all roles.

Implemented:

- `POST /api/auth/login`
- AuthService login validation.

BRD requirement: Forced password change.

Implemented:

- `ForcePasswordChange` returned on login.
- `POST /api/auth/change-password`.
- Password change clears the flag.

BRD requirement: Inactive accounts cannot log in.

Implemented:

- AuthService blocks inactive users.
- Tests verify this rule.

BRD requirement: First admin bootstrap.

Implemented:

- Seed admin has real PBKDF2 hash.
- Seed admin must still change password on first login.

## Day 4 / Next Work

Next work should continue user/account management:

1. Create user account API.
2. View users API.
3. Reset password API.
4. Deactivate user API.
5. Add role checks so Admin-only actions are protected.
6. Add console client login flow using these auth endpoints.

