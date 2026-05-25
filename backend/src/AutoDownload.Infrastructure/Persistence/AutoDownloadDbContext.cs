using AutoDownload.Application.Abstractions;
using AutoDownload.Domain.Entities;
using AutoDownload.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace AutoDownload.Infrastructure.Persistence;

public sealed class AutoDownloadDbContext : DbContext, IUnitOfWork
{
    public AutoDownloadDbContext(DbContextOptions<AutoDownloadDbContext> options)
        : base(options)
    {
    }

    public DbSet<AppUser> Users => Set<AppUser>();

    public DbSet<OperatorCompany> Operators => Set<OperatorCompany>();

    public DbSet<UserAccount> Accounts => Set<UserAccount>();

    public DbSet<Bill> Bills => Set<Bill>();

    public DbSet<AutomationRun> AutomationRuns => Set<AutomationRun>();

    public DbSet<Notification> Notifications => Set<Notification>();

    async Task IUnitOfWork.SaveChangesAsync(CancellationToken cancellationToken)
    {
        await SaveChangesAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigureUsers(modelBuilder);
        ConfigureOperators(modelBuilder);
        ConfigureAccounts(modelBuilder);
        ConfigureBills(modelBuilder);
        ConfigureAutomationRuns(modelBuilder);
        ConfigureNotifications(modelBuilder);
    }

    private static void ConfigureUsers(ModelBuilder modelBuilder)
    {
        var emailConverter = new ValueConverter<EmailAddress, string>(
            email => email.Value,
            value => EmailAddress.Create(value));

        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(user => user.Id);
            entity.Property(user => user.Id).ValueGeneratedNever();
            entity.Property(user => user.Name).HasMaxLength(120).IsRequired();
            entity.Property(user => user.Email).HasConversion(emailConverter).HasMaxLength(254).IsRequired();
            entity.HasIndex(user => user.Email).IsUnique();
            entity.Property(user => user.PasswordHash).HasMaxLength(500).IsRequired();
            entity.Property(user => user.CreatedAt).IsRequired();
            entity.Property(user => user.UpdatedAt).IsRequired();
        });
    }

    private static void ConfigureOperators(ModelBuilder modelBuilder)
    {
        var uriConverter = new ValueConverter<Uri, string>(
            uri => uri.ToString(),
            value => new Uri(value));

        modelBuilder.Entity<OperatorCompany>(entity =>
        {
            entity.ToTable("operators");
            entity.HasKey(operatorCompany => operatorCompany.Id);
            entity.Property(operatorCompany => operatorCompany.Id).ValueGeneratedNever();
            entity.Property(operatorCompany => operatorCompany.Code).HasMaxLength(50).IsRequired();
            entity.HasIndex(operatorCompany => operatorCompany.Code).IsUnique();
            entity.Property(operatorCompany => operatorCompany.Name).HasMaxLength(120).IsRequired();
            entity.Property(operatorCompany => operatorCompany.ServiceType).IsRequired();
            entity.Property(operatorCompany => operatorCompany.PortalBaseUrl).HasConversion(uriConverter).HasMaxLength(500).IsRequired();
            entity.Property(operatorCompany => operatorCompany.IsActive).IsRequired();
        });
    }

    private static void ConfigureAccounts(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserAccount>(entity =>
        {
            entity.ToTable("accounts");
            entity.HasKey(account => account.Id);
            entity.Property(account => account.Id).ValueGeneratedNever();
            entity.Property(account => account.UserId).IsRequired();
            entity.Property(account => account.OperatorId).IsRequired();
            entity.Property(account => account.PortalLogin).HasMaxLength(160).IsRequired();
            entity.Property(account => account.EncryptedPortalPassword).HasMaxLength(2000).IsRequired();
            entity.Property(account => account.CustomerIdentifier).HasMaxLength(80).IsRequired();
            entity.Property(account => account.Status).IsRequired();
            entity.Property(account => account.CreatedAt).IsRequired();
            entity.Property(account => account.UpdatedAt).IsRequired();
            entity.Property(account => account.LastRunAt);
            entity.Property(account => account.NextRunAt);
            entity.HasIndex(account => new { account.UserId, account.OperatorId, account.CustomerIdentifier }).IsUnique();
            entity.HasOne<AppUser>().WithMany().HasForeignKey(account => account.UserId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<OperatorCompany>().WithMany().HasForeignKey(account => account.OperatorId).OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureBills(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Bill>(entity =>
        {
            entity.ToTable("bills");
            entity.HasKey(bill => bill.Id);
            entity.Property(bill => bill.Id).ValueGeneratedNever();
            entity.Property(bill => bill.UserId).IsRequired();
            entity.Property(bill => bill.AccountId).IsRequired();
            entity.Property(bill => bill.OperatorId).IsRequired();
            entity.Property(bill => bill.Reference).HasMaxLength(30).IsRequired();
            entity.Property(bill => bill.DueDate).IsRequired();
            entity.Property(bill => bill.Amount).HasPrecision(12, 2).IsRequired();
            entity.Property(bill => bill.FileName).HasMaxLength(180).IsRequired();
            entity.Property(bill => bill.StoragePath).HasMaxLength(260).IsRequired();
            entity.Property(bill => bill.DownloadedAt).IsRequired();
            entity.Property(bill => bill.Status).IsRequired();
            entity.HasIndex(bill => new { bill.AccountId, bill.Reference }).IsUnique();
            entity.HasOne<AppUser>().WithMany().HasForeignKey(bill => bill.UserId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<UserAccount>().WithMany().HasForeignKey(bill => bill.AccountId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<OperatorCompany>().WithMany().HasForeignKey(bill => bill.OperatorId).OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureAutomationRuns(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AutomationRun>(entity =>
        {
            entity.ToTable("automation_runs");
            entity.HasKey(run => run.Id);
            entity.Property(run => run.Id).ValueGeneratedNever();
            entity.Property(run => run.UserId).IsRequired();
            entity.Property(run => run.AccountId).IsRequired();
            entity.Property(run => run.OperatorId).IsRequired();
            entity.Property(run => run.StartedAt).IsRequired();
            entity.Property(run => run.FinishedAt).IsRequired();
            entity.Property(run => run.Status).IsRequired();
            entity.Property(run => run.Message).HasMaxLength(500).IsRequired();
            entity.Property(run => run.FileName).HasMaxLength(180);
            entity.Ignore(run => run.DurationSeconds);
            entity.HasIndex(run => new { run.UserId, run.StartedAt });
            entity.HasOne<AppUser>().WithMany().HasForeignKey(run => run.UserId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<UserAccount>().WithMany().HasForeignKey(run => run.AccountId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<OperatorCompany>().WithMany().HasForeignKey(run => run.OperatorId).OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureNotifications(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Notification>(entity =>
        {
            entity.ToTable("notifications");
            entity.HasKey(notification => notification.Id);
            entity.Property(notification => notification.Id).ValueGeneratedNever();
            entity.Property(notification => notification.UserId).IsRequired();
            entity.Property(notification => notification.Text).HasMaxLength(300).IsRequired();
            entity.Property(notification => notification.Type).IsRequired();
            entity.Property(notification => notification.CreatedAt).IsRequired();
            entity.Property(notification => notification.ReadAt);
            entity.Ignore(notification => notification.IsRead);
            entity.HasIndex(notification => new { notification.UserId, notification.CreatedAt });
            entity.HasOne<AppUser>().WithMany().HasForeignKey(notification => notification.UserId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
