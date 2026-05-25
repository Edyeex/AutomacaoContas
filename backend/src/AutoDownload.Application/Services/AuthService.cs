using AutoDownload.Application.Abstractions;
using AutoDownload.Application.Common;
using AutoDownload.Application.Contracts;
using AutoDownload.Application.Mappings;
using AutoDownload.Domain.Common;
using AutoDownload.Domain.Entities;
using AutoDownload.Domain.Enums;
using AutoDownload.Domain.ValueObjects;

namespace AutoDownload.Application.Services;

public sealed class AuthService
{
    private readonly IUserRepository users;
    private readonly INotificationRepository notifications;
    private readonly IPasswordHasher passwordHasher;
    private readonly IAccessTokenService accessTokenService;
    private readonly IClock clock;
    private readonly IUnitOfWork unitOfWork;

    public AuthService(
        IUserRepository users,
        INotificationRepository notifications,
        IPasswordHasher passwordHasher,
        IAccessTokenService accessTokenService,
        IClock clock,
        IUnitOfWork unitOfWork)
    {
        this.users = users;
        this.notifications = notifications;
        this.passwordHasher = passwordHasher;
        this.accessTokenService = accessTokenService;
        this.clock = clock;
        this.unitOfWork = unitOfWork;
    }

    public async Task<Result<AuthResponse>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < AppUser.MinPasswordLength)
        {
            return Result<AuthResponse>.Failure(Error.Validation("auth.password_too_short", "Password must contain at least 6 characters."));
        }

        try
        {
            var email = EmailAddress.Create(request.Email);
            if (await users.EmailExistsAsync(email, cancellationToken: cancellationToken))
            {
                return Result<AuthResponse>.Failure(Error.Conflict("auth.email_in_use", "Email is already registered."));
            }

            var user = AppUser.Register(request.Name, email, passwordHasher.Hash(request.Password), clock.Now);
            await users.AddAsync(user, cancellationToken);
            await notifications.AddAsync(
                Notification.Create(user.Id, "Conta criada com sucesso.", NotificationType.Success, clock.Now),
                cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<AuthResponse>.Success(BuildAuthResponse(user));
        }
        catch (DomainException ex)
        {
            return Result<AuthResponse>.Failure(Error.Validation("auth.invalid_registration", ex.Message));
        }
    }

    public async Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        EmailAddress email;
        try
        {
            email = EmailAddress.Create(request.Email);
        }
        catch (DomainException)
        {
            return InvalidLogin();
        }

        var user = await users.FindByEmailAsync(email, cancellationToken);
        if (user is null)
        {
            return InvalidLogin();
        }

        var verification = passwordHasher.Verify(user.PasswordHash, request.Password);
        if (verification == PasswordCheckResult.Failed)
        {
            return InvalidLogin();
        }

        if (verification == PasswordCheckResult.SuccessRehashNeeded)
        {
            user.ChangePasswordHash(passwordHasher.Hash(request.Password), clock.Now);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return Result<AuthResponse>.Success(BuildAuthResponse(user));

        static Result<AuthResponse> InvalidLogin()
            => Result<AuthResponse>.Failure(Error.Unauthorized("auth.invalid_credentials", "Invalid email or password."));
    }

    public async Task<Result> RequestPasswordRecoveryAsync(PasswordRecoveryRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var email = EmailAddress.Create(request.Email);
            var user = await users.FindByEmailAsync(email, cancellationToken);
            if (user is not null)
            {
                await notifications.AddAsync(
                    Notification.Create(user.Id, "Solicitação de recuperação de senha registrada.", NotificationType.Info, clock.Now),
                    cancellationToken);
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }

            return Result.Success();
        }
        catch (DomainException)
        {
            return Result.Success();
        }
    }

    public async Task<Result<UserResponse>> GetProfileAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await users.FindByIdAsync(userId, cancellationToken);
        return user is null
            ? Result<UserResponse>.Failure(Error.NotFound("user.not_found", "User not found."))
            : Result<UserResponse>.Success(user.ToResponse());
    }

    public async Task<Result<UserResponse>> UpdateProfileAsync(Guid userId, UpdateProfileRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await users.FindByIdAsync(userId, cancellationToken);
            if (user is null)
            {
                return Result<UserResponse>.Failure(Error.NotFound("user.not_found", "User not found."));
            }

            var email = EmailAddress.Create(request.Email);
            if (await users.EmailExistsAsync(email, userId, cancellationToken))
            {
                return Result<UserResponse>.Failure(Error.Conflict("user.email_in_use", "Email is already registered by another user."));
            }

            user.UpdateProfile(request.Name, email, clock.Now);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<UserResponse>.Success(user.ToResponse());
        }
        catch (DomainException ex)
        {
            return Result<UserResponse>.Failure(Error.Validation("user.invalid_profile", ex.Message));
        }
    }

    public async Task<Result> ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < AppUser.MinPasswordLength)
        {
            return Result.Failure(Error.Validation("user.password_too_short", "New password must contain at least 6 characters."));
        }

        var user = await users.FindByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return Result.Failure(Error.NotFound("user.not_found", "User not found."));
        }

        if (passwordHasher.Verify(user.PasswordHash, request.CurrentPassword) == PasswordCheckResult.Failed)
        {
            return Result.Failure(Error.Unauthorized("user.invalid_current_password", "Current password is invalid."));
        }

        user.ChangePasswordHash(passwordHasher.Hash(request.NewPassword), clock.Now);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    private AuthResponse BuildAuthResponse(AppUser user)
    {
        var token = accessTokenService.Issue(user);
        return new AuthResponse(user.ToResponse(), token.Token, token.ExpiresAt);
    }
}
