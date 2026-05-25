using System.Text.Json;
using System.Text.Json.Serialization;
using AutoDownload.Api.Extensions;
using AutoDownload.Application.Contracts;
using AutoDownload.Application.Services;
using AutoDownload.Infrastructure;
using AutoDownload.Infrastructure.Persistence;
using AutoDownload.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
});

var tokenOptions = builder.Configuration.GetSection("Security:AccessToken").Get<AccessTokenOptions>()
    ?? new AccessTokenOptions();

builder.Services.AddCors(options =>
{
    options.AddPolicy("frontend", policy =>
    {
        var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? ["http://localhost:3000"];

        policy
            .WithOrigins(origins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = JwtAccessTokenService.BuildValidationParameters(tokenOptions);
        options.MapInboundClaims = false;
    });

builder.Services.AddAuthorization();

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<AccountService>();
builder.Services.AddScoped<BillService>();
builder.Services.AddScoped<HistoryService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<DashboardService>();
builder.Services.AddScoped<AutomationOrchestrator>();

var app = builder.Build();

if (app.Configuration.GetValue<bool>("Database:ApplyMigrationsOnStartup"))
{
    await app.Services.ApplyMigrationsAndSeedAsync();
}

app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Unhandled API exception.");
        if (!context.Response.HasStarted)
        {
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsJsonAsync(new
            {
                title = "api.unexpected_error",
                detail = "Unexpected server error."
            });
        }
    }
});

app.UseCors("frontend");
app.UseAuthentication();
app.UseAuthorization();

var api = app.MapGroup("/api");

api.MapGet("/health", () => Results.Ok(new
{
    status = "healthy",
    application = "AutoDownload.Api",
    runtime = ".NET 10"
}));

var auth = api.MapGroup("/auth");

auth.MapPost("/register", async (RegisterRequest request, AuthService service, CancellationToken cancellationToken)
    => (await service.RegisterAsync(request, cancellationToken)).ToHttpResult());

auth.MapPost("/login", async (LoginRequest request, AuthService service, CancellationToken cancellationToken)
    => (await service.LoginAsync(request, cancellationToken)).ToHttpResult());

auth.MapPost("/recover-password", async (PasswordRecoveryRequest request, AuthService service, CancellationToken cancellationToken)
    => (await service.RequestPasswordRecoveryAsync(request, cancellationToken)).ToHttpResult());

var secure = api.MapGroup("").RequireAuthorization();

secure.MapGet("/me", async (HttpContext context, AuthService service, CancellationToken cancellationToken)
    => (await service.GetProfileAsync(context.User.GetRequiredUserId(), cancellationToken)).ToHttpResult());

secure.MapPut("/me", async (UpdateProfileRequest request, HttpContext context, AuthService service, CancellationToken cancellationToken)
    => (await service.UpdateProfileAsync(context.User.GetRequiredUserId(), request, cancellationToken)).ToHttpResult());

secure.MapPut("/me/password", async (ChangePasswordRequest request, HttpContext context, AuthService service, CancellationToken cancellationToken)
    => (await service.ChangePasswordAsync(context.User.GetRequiredUserId(), request, cancellationToken)).ToHttpResult());

secure.MapGet("/dashboard", async (HttpContext context, DashboardService service, CancellationToken cancellationToken)
    => (await service.GetAsync(context.User.GetRequiredUserId(), cancellationToken)).ToHttpResult());

secure.MapGet("/operators", async (AccountService service, CancellationToken cancellationToken)
    => (await service.ListOperatorsAsync(cancellationToken)).ToHttpResult());

secure.MapGet("/accounts", async (HttpContext context, AccountService service, CancellationToken cancellationToken)
    => (await service.ListAsync(context.User.GetRequiredUserId(), cancellationToken)).ToHttpResult());

secure.MapPost("/accounts", async (AccountCreateRequest request, HttpContext context, AccountService service, CancellationToken cancellationToken)
    => (await service.CreateAsync(context.User.GetRequiredUserId(), request, cancellationToken)).ToHttpResult());

secure.MapPut("/accounts/{accountId:guid}", async (
        Guid accountId,
        AccountUpdateRequest request,
        HttpContext context,
        AccountService service,
        CancellationToken cancellationToken)
    => (await service.UpdateAsync(context.User.GetRequiredUserId(), accountId, request, cancellationToken)).ToHttpResult());

secure.MapDelete("/accounts/{accountId:guid}", async (
        Guid accountId,
        HttpContext context,
        AccountService service,
        CancellationToken cancellationToken)
    => (await service.DeleteAsync(context.User.GetRequiredUserId(), accountId, cancellationToken)).ToHttpResult());

secure.MapPost("/accounts/{accountId:guid}/run", async (
        Guid accountId,
        HttpContext context,
        AutomationOrchestrator orchestrator,
        CancellationToken cancellationToken)
    => (await orchestrator.RunAccountAsync(context.User.GetRequiredUserId(), accountId, cancellationToken)).ToHttpResult());

secure.MapGet("/bills", async (HttpContext context, BillService service, CancellationToken cancellationToken)
    => (await service.ListAsync(context.User.GetRequiredUserId(), cancellationToken)).ToHttpResult());

secure.MapGet("/bills/{billId:guid}/download", async (
        Guid billId,
        HttpContext context,
        BillService service,
        CancellationToken cancellationToken)
    =>
{
    var result = await service.GetFileAsync(context.User.GetRequiredUserId(), billId, cancellationToken);
    if (result.IsFailure)
    {
        return result.ToHttpResult();
    }

    var file = result.Value;
    if (!System.IO.File.Exists(file.StoragePath))
    {
        return Results.Problem(
            statusCode: StatusCodes.Status404NotFound,
            title: "bill.file_not_found",
            detail: "Bill file was registered, but it was not found on disk.");
    }

    return Results.File(file.StoragePath, "application/pdf", file.FileName);
});

secure.MapGet("/history", async (string? status, HttpContext context, HistoryService service, CancellationToken cancellationToken)
    => (await service.ListAsync(context.User.GetRequiredUserId(), status, cancellationToken)).ToHttpResult());

secure.MapGet("/notifications", async (HttpContext context, NotificationService service, CancellationToken cancellationToken)
    => (await service.ListAsync(context.User.GetRequiredUserId(), cancellationToken)).ToHttpResult());

secure.MapPatch("/notifications/mark-all-read", async (HttpContext context, NotificationService service, CancellationToken cancellationToken)
    => (await service.MarkAllAsReadAsync(context.User.GetRequiredUserId(), cancellationToken)).ToHttpResult());

secure.MapDelete("/notifications", async (HttpContext context, NotificationService service, CancellationToken cancellationToken)
    => (await service.DeleteAllAsync(context.User.GetRequiredUserId(), cancellationToken)).ToHttpResult());

secure.MapPatch("/notifications/{notificationId:guid}/read", async (
        Guid notificationId,
        ReadNotificationRequest request,
        HttpContext context,
        NotificationService service,
        CancellationToken cancellationToken)
    => (await service.MarkAsReadAsync(context.User.GetRequiredUserId(), notificationId, request.Read, cancellationToken)).ToHttpResult());

secure.MapDelete("/notifications/{notificationId:guid}", async (
        Guid notificationId,
        HttpContext context,
        NotificationService service,
        CancellationToken cancellationToken)
    => (await service.DeleteAsync(context.User.GetRequiredUserId(), notificationId, cancellationToken)).ToHttpResult());

app.Run();

internal sealed record ReadNotificationRequest(bool Read);
