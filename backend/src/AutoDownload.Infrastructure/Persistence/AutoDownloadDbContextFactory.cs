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

        var connectionString = configuration.GetConnectionString("AutoDownload")
            ?? "Host=localhost;Port=5432;Database=autodownload;Username=autodownload;Password=autodownload";

        var options = new DbContextOptionsBuilder<AutoDownloadDbContext>()
            .UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsAssembly(typeof(AutoDownloadDbContext).Assembly.FullName))
            .Options;

        return new AutoDownloadDbContext(options);
    }
}
