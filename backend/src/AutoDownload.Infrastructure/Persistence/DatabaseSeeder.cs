using AutoDownload.Application.Abstractions;
using AutoDownload.Domain.Entities;
using AutoDownload.Domain.Enums;
using AutoDownload.Domain.ValueObjects;
using AutoDownload.Infrastructure.Automation;
using Microsoft.EntityFrameworkCore;

namespace AutoDownload.Infrastructure.Persistence;

internal sealed class DatabaseSeeder
{
    private static readonly Guid DemoUserId = Guid.Parse("6bc6dc2b-60ce-4f9a-87db-e24483e98412");
    private static readonly Guid CeeeId = Guid.Parse("fd289a11-0b59-4f3d-84e4-4efeb19d8aac");
    private static readonly Guid CorsanId = Guid.Parse("6a220e0d-42b4-4cab-9723-aa10ab398083");
    private static readonly Guid VeroId = Guid.Parse("19783f49-1ef3-49ce-af82-e2068126ff7f");
    private static readonly Guid RmsId = Guid.Parse("4dd22bb7-875a-4811-8b78-2c1cc4c6df26");
    private static readonly Guid DemoOperatorId = Guid.Parse("0d2ae631-e8a5-4df6-9068-9286047de9cf");
    private static readonly Guid LegacyClaroId = Guid.Parse("bdb01b8f-39c5-4f58-9f6c-f9839e118f8b");
    private static readonly Guid CeeeAccountId = Guid.Parse("dcd144e0-82f9-4c60-8a91-3ea9de5be1af");
    private static readonly Guid CorsanAccountId = Guid.Parse("67d8eeb1-915e-4dcb-a3a0-a012fa476b97");
    private static readonly TimeSpan SaoPauloOffset = TimeSpan.FromHours(-3);

    private readonly AutoDownloadDbContext dbContext;
    private readonly IPasswordHasher passwordHasher;
    private readonly ICredentialProtector credentialProtector;
    private readonly IClock clock;
    private readonly IDemoBillPdfGenerator demoBillPdfGenerator;

    public DatabaseSeeder(
        AutoDownloadDbContext dbContext,
        IPasswordHasher passwordHasher,
        ICredentialProtector credentialProtector,
        IClock clock,
        IDemoBillPdfGenerator demoBillPdfGenerator)
    {
        this.dbContext = dbContext;
        this.passwordHasher = passwordHasher;
        this.credentialProtector = credentialProtector;
        this.clock = clock;
        this.demoBillPdfGenerator = demoBillPdfGenerator;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var timeline = SeedTimeline.From(clock.Now);
        var demoEmail = EmailAddress.Create("eder.casagranda@email.com");

        await EnsureOperatorAsync(
            new OperatorCompany(CeeeId, "ceee-equatorial", "CEEE Equatorial", ServiceType.Energy, new Uri("https://ceee.equatorialenergia.com.br/"), true),
            cancellationToken);
        await EnsureOperatorAsync(
            new OperatorCompany(CorsanId, "corsan", "CORSAN", ServiceType.Water, new Uri("https://www.corsan.com.br/"), true),
            cancellationToken);
        await EnsureOperatorAsync(
            new OperatorCompany(VeroId, "vero-internet", "Vero Internet", ServiceType.Internet, new Uri("https://verointernet.com.br/minhavero/login"), true),
            cancellationToken);
        await EnsureOperatorAsync(
            new OperatorCompany(RmsId, "rms-telecom", "RMS Telecom", ServiceType.Internet, new Uri("https://fatura.rmstelecom.net/login"), true),
            cancellationToken);
        await EnsureOperatorAsync(
            new OperatorCompany(DemoOperatorId, DemoOperatorAutomationStrategy.OperatorCode, "Operador Demo", ServiceType.Internet, new Uri("https://demo.autodownload.local/"), true),
            cancellationToken);

        await DeactivateLegacyClaroSeedAsync(timeline.Now, cancellationToken);
        var demoUserCreated = await EnsureDemoUserAsync(demoEmail, timeline, cancellationToken);
        if (demoUserCreated)
        {
            await EnsureDemoAccountsAsync(timeline, cancellationToken);
            await RefreshDemoOperationalDataAsync(timeline, cancellationToken);
        }
        else
        {
            await EnsureDemoBillFilesAsync(cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureOperatorAsync(OperatorCompany operatorCompany, CancellationToken cancellationToken)
    {
        var existing = await dbContext.Operators.FirstOrDefaultAsync(
            item => item.Id == operatorCompany.Id || item.Code == operatorCompany.Code,
            cancellationToken);

        if (existing is null)
        {
            dbContext.Operators.Add(operatorCompany);
            return;
        }

        if (!existing.IsActive)
        {
            existing.Activate();
        }
    }

    private async Task<bool> EnsureDemoUserAsync(
        EmailAddress demoEmail,
        SeedTimeline timeline,
        CancellationToken cancellationToken)
    {
        var userExists = await dbContext.Users.AnyAsync(
            user => user.Id == DemoUserId || user.Email == demoEmail,
            cancellationToken);

        if (!userExists)
        {
            dbContext.Users.Add(new AppUser(
                DemoUserId,
                "Éder Casagranda",
                demoEmail,
                passwordHasher.Hash("123456"),
                timeline.UserCreatedAt,
                timeline.UserCreatedAt));
            return true;
        }

        return false;
    }

    private async Task EnsureDemoAccountsAsync(SeedTimeline timeline, CancellationToken cancellationToken)
    {
        await EnsureDemoAccountAsync(
            CeeeAccountId,
            CeeeId,
            "eder.casagranda@email.com",
            "portal-demo-123",
            "UC-4821903",
            timeline,
            cancellationToken);
        await EnsureDemoAccountAsync(
            CorsanAccountId,
            CorsanId,
            "eder_corsan",
            "portal-demo-123",
            "MAT-77210",
            timeline,
            cancellationToken);
    }

    private async Task EnsureDemoAccountAsync(
        Guid accountId,
        Guid operatorId,
        string portalLogin,
        string portalPassword,
        string customerIdentifier,
        SeedTimeline timeline,
        CancellationToken cancellationToken)
    {
        var accountExists = await dbContext.Accounts.AnyAsync(
            account => account.Id == accountId,
            cancellationToken);

        if (!accountExists)
        {
            dbContext.Accounts.Add(new UserAccount(
                accountId,
                DemoUserId,
                operatorId,
                portalLogin,
                credentialProtector.Protect(portalPassword),
                customerIdentifier,
                AccountStatus.Active,
                timeline.AccountCreatedAt,
                timeline.AccountUpdatedAt,
                timeline.LastRunAt,
                timeline.NextRunAt));
            return;
        }

        var protectedPassword = credentialProtector.Protect(portalPassword);
        await dbContext.Accounts
            .Where(account => account.Id == accountId)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(account => account.UserId, DemoUserId)
                    .SetProperty(account => account.OperatorId, operatorId)
                    .SetProperty(account => account.PortalLogin, portalLogin)
                    .SetProperty(account => account.EncryptedPortalPassword, protectedPassword)
                    .SetProperty(account => account.CustomerIdentifier, customerIdentifier)
                    .SetProperty(account => account.Status, AccountStatus.Active)
                    .SetProperty(account => account.CreatedAt, timeline.AccountCreatedAt)
                    .SetProperty(account => account.UpdatedAt, timeline.AccountUpdatedAt)
                    .SetProperty(account => account.LastRunAt, timeline.LastRunAt)
                    .SetProperty(account => account.NextRunAt, timeline.NextRunAt),
                cancellationToken);
    }

    private async Task RefreshDemoOperationalDataAsync(SeedTimeline timeline, CancellationToken cancellationToken)
    {
        var demoAccountIds = new[] { CeeeAccountId, CorsanAccountId };

        await dbContext.Bills
            .Where(bill =>
                bill.UserId == DemoUserId &&
                demoAccountIds.Contains(bill.AccountId) &&
                (bill.FileName.StartsWith("ceee_") || bill.FileName.StartsWith("corsan_")))
            .ExecuteDeleteAsync(cancellationToken);

        await dbContext.AutomationRuns
            .Where(run =>
                run.UserId == DemoUserId &&
                demoAccountIds.Contains(run.AccountId) &&
                (run.FileName == null || run.FileName.StartsWith("ceee_") || run.FileName.StartsWith("corsan_")))
            .ExecuteDeleteAsync(cancellationToken);

        await dbContext.Notifications
            .Where(notification =>
                notification.UserId == DemoUserId &&
                (notification.Text.Contains("CEEE") || notification.Text.Contains("CORSAN")))
            .ExecuteDeleteAsync(cancellationToken);

        dbContext.Bills.AddRange(
            await BuildBillAsync(CeeeAccountId, CeeeId, "CEEE Equatorial", ServiceType.Energy, "ceee", 187.42m, timeline.CurrentReference, timeline.CurrentDownloadAt, cancellationToken),
            await BuildBillAsync(CorsanAccountId, CorsanId, "CORSAN", ServiceType.Water, "corsan", 94.18m, timeline.CurrentReference, timeline.CurrentDownloadAt.AddMinutes(2), cancellationToken),
            await BuildBillAsync(CeeeAccountId, CeeeId, "CEEE Equatorial", ServiceType.Energy, "ceee", 203.55m, timeline.PreviousReference, timeline.PreviousDownloadAt, cancellationToken),
            await BuildBillAsync(CorsanAccountId, CorsanId, "CORSAN", ServiceType.Water, "corsan", 88.30m, timeline.PreviousReference, timeline.PreviousDownloadAt.AddMinutes(2), cancellationToken));

        dbContext.AutomationRuns.AddRange(
            AutomationRun.Create(DemoUserId, CeeeAccountId, CeeeId, timeline.CurrentDownloadAt, timeline.CurrentDownloadAt.AddSeconds(12), AutomationRunStatus.Success, $"Boleto de {BillReference.FromDate(timeline.CurrentReference)} baixado com sucesso.", FileName("ceee", timeline.CurrentReference)),
            AutomationRun.Create(DemoUserId, CorsanAccountId, CorsanId, timeline.CurrentDownloadAt.AddMinutes(2), timeline.CurrentDownloadAt.AddMinutes(2).AddSeconds(8), AutomationRunStatus.Success, $"Boleto de {BillReference.FromDate(timeline.CurrentReference)} baixado com sucesso.", FileName("corsan", timeline.CurrentReference)),
            AutomationRun.Create(DemoUserId, CeeeAccountId, CeeeId, timeline.FailedRunAt, timeline.FailedRunAt.AddSeconds(30), AutomationRunStatus.ConnectionError, "Timeout ao carregar portal. Tentativa será repetida.", null),
            AutomationRun.Create(DemoUserId, CeeeAccountId, CeeeId, timeline.PreviousDownloadAt, timeline.PreviousDownloadAt.AddSeconds(11), AutomationRunStatus.Success, $"Boleto de {BillReference.FromDate(timeline.PreviousReference)} baixado com sucesso.", FileName("ceee", timeline.PreviousReference)),
            AutomationRun.Create(DemoUserId, CorsanAccountId, CorsanId, timeline.PreviousDownloadAt.AddMinutes(2), timeline.PreviousDownloadAt.AddMinutes(2).AddSeconds(7), AutomationRunStatus.Success, $"Boleto de {BillReference.FromDate(timeline.PreviousReference)} baixado com sucesso.", FileName("corsan", timeline.PreviousReference)));

        dbContext.Notifications.AddRange(
            Notification.Create(DemoUserId, $"Boleto CEEE Equatorial ({BillReference.FromDate(timeline.CurrentReference)}) baixado com sucesso.", NotificationType.Success, timeline.CurrentDownloadAt),
            Notification.Create(DemoUserId, $"Boleto CORSAN ({BillReference.FromDate(timeline.CurrentReference)}) baixado com sucesso.", NotificationType.Success, timeline.CurrentDownloadAt.AddMinutes(2)),
            new Notification(Guid.NewGuid(), DemoUserId, "Falha ao acessar portal CEEE Equatorial. Nova tentativa agendada.", NotificationType.Warning, timeline.FailedRunAt, timeline.FailedRunAt.AddDays(1)));
    }

    private async Task DeactivateLegacyClaroSeedAsync(
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        _ = now;

        var legacyOperatorIds = await dbContext.Operators
            .Where(item => item.Id == LegacyClaroId || item.Code == "claro")
            .Select(item => item.Id)
            .ToListAsync(cancellationToken);
        if (legacyOperatorIds.Count == 0)
        {
            return;
        }

        var legacyAccountIds = await dbContext.Accounts
            .Where(item => legacyOperatorIds.Contains(item.OperatorId))
            .Select(item => item.Id)
            .ToListAsync(cancellationToken);

        await dbContext.Bills
            .Where(item => legacyOperatorIds.Contains(item.OperatorId) || legacyAccountIds.Contains(item.AccountId))
            .ExecuteDeleteAsync(cancellationToken);

        await dbContext.AutomationRuns
            .Where(item => legacyOperatorIds.Contains(item.OperatorId) || legacyAccountIds.Contains(item.AccountId))
            .ExecuteDeleteAsync(cancellationToken);

        await dbContext.Accounts
            .Where(item => legacyAccountIds.Contains(item.Id))
            .ExecuteDeleteAsync(cancellationToken);

        await dbContext.Notifications
            .Where(item => item.Text.Contains("Claro"))
            .ExecuteDeleteAsync(cancellationToken);

        await dbContext.Operators
            .Where(item => legacyOperatorIds.Contains(item.Id))
            .ExecuteDeleteAsync(cancellationToken);
    }

    private async Task EnsureDemoBillFilesAsync(CancellationToken cancellationToken)
    {
        var demoAccountIds = new[] { CeeeAccountId, CorsanAccountId };
        var demoBills = await dbContext.Bills
            .Where(bill => bill.UserId == DemoUserId && demoAccountIds.Contains(bill.AccountId))
            .ToListAsync(cancellationToken);

        foreach (var bill in demoBills)
        {
            if (File.Exists(bill.StoragePath))
            {
                continue;
            }

            var operatorName = bill.OperatorId == CeeeId ? "CEEE Equatorial" : "CORSAN";
            var storagePath = await demoBillPdfGenerator.GenerateAsync(
                new DemoBillDocument(
                    bill.UserId,
                    bill.AccountId,
                    operatorName,
                    bill.Reference,
                    bill.DueDate,
                    bill.Amount,
                    bill.FileName),
                cancellationToken);

            bill.RefreshDownload(
                bill.DueDate,
                bill.Amount,
                bill.FileName,
                storagePath,
                bill.DownloadedAt);
        }
    }

    private async Task<Bill> BuildBillAsync(
        Guid accountId,
        Guid operatorId,
        string operatorName,
        ServiceType serviceType,
        string prefix,
        decimal amount,
        DateOnly reference,
        DateTimeOffset downloadedAt,
        CancellationToken cancellationToken)
    {
        var billReference = BillReference.FromDate(reference);
        var dueDate = BuildDueDate(serviceType, reference);
        var fileName = FileName(prefix, reference);
        var storagePath = await demoBillPdfGenerator.GenerateAsync(
            new DemoBillDocument(
                DemoUserId,
                accountId,
                operatorName,
                billReference,
                dueDate,
                amount,
                fileName),
            cancellationToken);

        return Bill.Create(
            DemoUserId,
            accountId,
            operatorId,
            billReference,
            dueDate,
            amount,
            fileName,
            storagePath,
            downloadedAt);
    }

    private static DateOnly BuildDueDate(ServiceType serviceType, DateOnly reference)
    {
        var day = serviceType switch
        {
            ServiceType.Energy => 10,
            ServiceType.Water => 15,
            ServiceType.Internet => 20,
            _ => 25
        };

        return new DateOnly(reference.Year, reference.Month, day).AddMonths(1);
    }

    private static string FileName(string prefix, DateOnly reference)
        => $"{prefix}_{reference:yyyy_MM}.pdf";

    private sealed record SeedTimeline(
        DateTimeOffset Now,
        DateTimeOffset UserCreatedAt,
        DateTimeOffset AccountCreatedAt,
        DateTimeOffset AccountUpdatedAt,
        DateTimeOffset LastRunAt,
        DateTimeOffset NextRunAt,
        DateTimeOffset CurrentDownloadAt,
        DateTimeOffset PreviousDownloadAt,
        DateTimeOffset FailedRunAt,
        DateOnly CurrentReference,
        DateOnly PreviousReference)
    {
        public static SeedTimeline From(DateTimeOffset utcNow)
        {
            var localNow = utcNow.ToOffset(SaoPauloOffset);
            var currentReference = new DateOnly(localNow.Year, localNow.Month, 1);
            var previousReference = currentReference.AddMonths(-1);
            var currentDownloadAt = AtLocalTime(localNow, 9, 0).AddDays(-2);
            var previousDownloadAt = currentDownloadAt.AddMonths(-1);
            var failedRunAt = currentDownloadAt.AddDays(-5);

            return new SeedTimeline(
                utcNow.ToUniversalTime(),
                currentDownloadAt.AddDays(-35).ToUniversalTime(),
                currentDownloadAt.AddDays(-28).ToUniversalTime(),
                currentDownloadAt.AddDays(-1).ToUniversalTime(),
                currentDownloadAt.ToUniversalTime(),
                currentDownloadAt.AddDays(5).ToUniversalTime(),
                currentDownloadAt.ToUniversalTime(),
                previousDownloadAt.ToUniversalTime(),
                failedRunAt.ToUniversalTime(),
                currentReference,
                previousReference);
        }

        private static DateTimeOffset AtLocalTime(DateTimeOffset localNow, int hour, int minute)
            => new(localNow.Year, localNow.Month, localNow.Day, hour, minute, 0, SaoPauloOffset);
    }
}
