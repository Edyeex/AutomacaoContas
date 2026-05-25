namespace AutoDownload.Application.Contracts;

public sealed record RegisterRequest(string Name, string Email, string Password);

public sealed record LoginRequest(string Email, string Password);

public sealed record PasswordRecoveryRequest(string Email);

public sealed record UpdateProfileRequest(string Name, string Email);

public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);

public sealed record UserResponse(Guid Id, string Name, string Email, string Initials, DateTimeOffset CreatedAt);

public sealed record AuthResponse(UserResponse User, string AccessToken, DateTimeOffset ExpiresAt);
