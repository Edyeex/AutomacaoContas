using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace AutoDownload.Infrastructure.Persistence;

public sealed class AutoDownloadDbContextFactory : IDesignTimeDbContextFactory<AutoDownloadDbContext>
{
    public AutoDownloadDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("AutoDownload");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Connection string 'AutoDownload' is required. Configure it with user-secrets or " +
                "the ConnectionStrings__AutoDownload environment variable.");
        }

        var options = new DbContextOptionsBuilder<AutoDownloadDbContext>()
            .UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsAssembly(typeof(AutoDownloadDbContext).Assembly.FullName))
            .Options;

        return new AutoDownloadDbContext(options);
    }
}
