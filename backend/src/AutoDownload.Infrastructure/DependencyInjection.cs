using AutoDownload.Application.Abstractions;
using AutoDownload.Infrastructure.Automation;
using AutoDownload.Infrastructure.Persistence;
using AutoDownload.Infrastructure.Security;
using AutoDownload.Infrastructure.Time;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AutoDownload.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AccessTokenOptions>(configuration.GetSection("Security:AccessToken"));
        services.Configure<VeroInternetAutomationOptions>(configuration.GetSection("Automation:VeroInternet"));
        services.Configure<RmsTelecomAutomationOptions>(configuration.GetSection("Automation:RmsTelecom"));

        services.AddDataProtection().SetApplicationName("AutoDownload");

        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IPasswordHasher, AspNetPasswordHasher>();
        services.AddSingleton<ICredentialProtector, DataProtectionCredentialProtector>();
        services.AddSingleton<IAccessTokenService, JwtAccessTokenService>();

        var connectionString = configuration.GetConnectionString("AutoDownload")
            ?? throw new InvalidOperationException("Connection string 'AutoDownload' is required.");

        services.AddDbContext<AutoDownloadDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsAssembly(typeof(AutoDownloadDbContext).Assembly.FullName));
        });

        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<AutoDownloadDbContext>());
        services.AddScoped<IUserRepository, EfUserRepository>();
        services.AddScoped<IOperatorRepository, EfOperatorRepository>();
        services.AddScoped<IUserAccountRepository, EfUserAccountRepository>();
        services.AddScoped<IBillRepository, EfBillRepository>();
        services.AddScoped<IAutomationRunRepository, EfAutomationRunRepository>();
        services.AddScoped<INotificationRepository, EfNotificationRepository>();
        services.AddScoped<DatabaseSeeder>();

        services.AddSingleton<IOperatorAutomationStrategy, VeroInternetAutomationStrategy>();
        services.AddSingleton<IOperatorAutomationStrategy, RmsTelecomAutomationStrategy>();
        services.AddSingleton<IOperatorAutomationStrategy, DemoOperatorAutomationStrategy>();
        services.AddSingleton<IOperatorAutomationStrategyResolver, OperatorAutomationStrategyResolver>();

        if (configuration.GetValue<bool>("Automation:EnableScheduler"))
        {
            services.AddHostedService<AutomationSchedulerService>();
        }

        return services;
    }
}
