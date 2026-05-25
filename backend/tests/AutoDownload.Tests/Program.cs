using AutoDownload.Domain.Entities;
using AutoDownload.Domain.Enums;
using AutoDownload.Domain.ValueObjects;

var now = new DateTimeOffset(2026, 5, 19, 12, 0, 0, TimeSpan.Zero);
var user = AppUser.Register(
    "Maria Silva",
    EmailAddress.Create("MARIA.SILVA@example.com"),
    "hashed-password",
    now);

Should(user.Email.Value == "maria.silva@example.com", "Email must be normalized.");
Should(UserAccount.MaxActiveAccountsPerUser == 3, "Domain must keep the documented 3-account limit.");

var account = UserAccount.Create(
    user.Id,
    Guid.NewGuid(),
    "portal.login",
    "encrypted-secret",
    "UC-0001",
    now,
    now.AddDays(5));

account.MarkAutomationRun(now.AddMinutes(1), now.AddDays(6));
Should(account.LastRunAt == now.AddMinutes(1), "Account must track the last automation run.");

var bill = Bill.Create(
    user.Id,
    account.Id,
    account.OperatorId,
    BillReference.FromDate(new DateOnly(2026, 5, 19)),
    new DateOnly(2026, 6, 10),
    123.456m,
    "boleto.pdf",
    "/storage/boleto.pdf",
    now);

Should(bill.Amount == 123.46m, "Bill amount must be rounded to two decimal places.");
bill.RefreshDownload(
    new DateOnly(2026, 6, 12),
    98.765m,
    "boleto-atualizado.pdf",
    "/storage/boleto-atualizado.pdf",
    now.AddMinutes(5));
Should(bill.Amount == 98.77m, "Bill refresh must update and round the amount.");
Should(bill.FileName == "boleto-atualizado.pdf", "Bill refresh must update file metadata.");

var run = AutomationRun.Create(
    user.Id,
    account.Id,
    account.OperatorId,
    now,
    now.AddSeconds(13),
    AutomationRunStatus.Success,
    "Boleto baixado com sucesso.",
    bill.FileName);

Should(run.DurationSeconds == 13, "Automation run duration must be calculated in seconds.");

var notification = Notification.Create(user.Id, "Boleto disponível.", NotificationType.Success, now);
Should(!notification.IsRead, "New notifications must start unread.");
notification.MarkAsRead(now.AddMinutes(2));
Should(notification.IsRead, "Notification must be marked as read.");

Console.WriteLine("AutoDownload backend checks passed.");

static void Should(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}
