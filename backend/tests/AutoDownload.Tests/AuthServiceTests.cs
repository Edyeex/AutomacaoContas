using AutoDownload.Application.Common;
using AutoDownload.Application.Contracts;
using AutoDownload.Application.Services;
using AutoDownload.Application.Abstractions;

namespace AutoDownload.Tests;

public sealed class AuthServiceTests
{
    [Fact]
    public async Task RegisterAsync_WithValidRequest_PersistsUserAndWelcomeNotification()
    {
        var users = new FakeUserRepository();
        var notifications = new FakeNotificationRepository();
        var passwordHasher = new FakePasswordHasher();
        var tokens = new FakeAccessTokenService();
        var unitOfWork = new FakeUnitOfWork();
        var service = new AuthService(
            users,
            notifications,
            passwordHasher,
            tokens,
            new FakeClock(),
            unitOfWork);

        var result = await service.RegisterAsync(
            new RegisterRequest("Maria Silva", "MARIA@example.com", "secret123"));

        Assert.True(result.IsSuccess);
        var user = Assert.Single(users.Items);
        Assert.Equal("maria@example.com", user.Email.Value);
        Assert.Equal("hash::secret123", user.PasswordHash);
        Assert.Equal("test-access-token", result.Value.AccessToken);
        Assert.Contains("Conta criada", Assert.Single(notifications.Items).Text, StringComparison.Ordinal);
        Assert.Equal(1, unitOfWork.SaveChangesCalls);
        Assert.Equal(1, tokens.IssueCalls);
    }

    [Fact]
    public async Task RegisterAsync_WithExistingEmail_ReturnsConflictWithoutSaving()
    {
        var users = new FakeUserRepository();
        users.Items.Add(TestData.User(email: "maria@example.com"));
        var unitOfWork = new FakeUnitOfWork();
        var service = new AuthService(
            users,
            new FakeNotificationRepository(),
            new FakePasswordHasher(),
            new FakeAccessTokenService(),
            new FakeClock(),
            unitOfWork);

        var result = await service.RegisterAsync(
            new RegisterRequest("Outra Maria", "MARIA@example.com", "secret123"));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.Error?.Type);
        Assert.Equal("auth.email_in_use", result.Error?.Code);
        Assert.Single(users.Items);
        Assert.Equal(0, unitOfWork.SaveChangesCalls);
    }

    [Fact]
    public async Task LoginAsync_WithInvalidPassword_DoesNotIssueToken()
    {
        var users = new FakeUserRepository();
        users.Items.Add(TestData.User(email: "maria@example.com"));
        var passwordHasher = new FakePasswordHasher
        {
            VerificationResult = PasswordCheckResult.Failed
        };
        var tokens = new FakeAccessTokenService();
        var service = new AuthService(
            users,
            new FakeNotificationRepository(),
            passwordHasher,
            tokens,
            new FakeClock(),
            new FakeUnitOfWork());

        var result = await service.LoginAsync(new LoginRequest("maria@example.com", "wrong-password"));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Unauthorized, result.Error?.Type);
        Assert.Equal("auth.invalid_credentials", result.Error?.Code);
        Assert.Equal(0, tokens.IssueCalls);
    }
}
