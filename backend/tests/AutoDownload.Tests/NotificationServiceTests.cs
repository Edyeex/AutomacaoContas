using AutoDownload.Application.Services;
using AutoDownload.Domain.Entities;
using AutoDownload.Domain.Enums;

namespace AutoDownload.Tests;

public sealed class NotificationServiceTests
{
    [Fact]
    public async Task MarkAllAsReadAsync_MarksEveryUnreadNotificationAndSavesOnce()
    {
        var userId = Guid.NewGuid();
        var notifications = new FakeNotificationRepository();
        var unread = Notification.Create(userId, "Nova fatura disponível.", NotificationType.Info, TestData.Now);
        var alreadyRead = new Notification(
            Guid.NewGuid(),
            userId,
            "Fatura anterior.",
            NotificationType.Success,
            TestData.Now.AddDays(-1),
            TestData.Now.AddHours(-1));
        notifications.Items.AddRange([unread, alreadyRead]);
        var unitOfWork = new FakeUnitOfWork();
        var service = new NotificationService(notifications, new FakeClock(), unitOfWork);

        var result = await service.MarkAllAsReadAsync(userId);

        Assert.True(result.IsSuccess);
        Assert.All(notifications.Items, notification => Assert.True(notification.IsRead));
        Assert.Equal(TestData.Now, unread.ReadAt);
        Assert.Equal(1, unitOfWork.SaveChangesCalls);
    }

    [Fact]
    public async Task DeleteAllAsync_RemovesOnlyCurrentUserNotifications()
    {
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var notifications = new FakeNotificationRepository();
        notifications.Items.Add(Notification.Create(userId, "Primeira notificação.", NotificationType.Info, TestData.Now));
        notifications.Items.Add(Notification.Create(userId, "Segunda notificação.", NotificationType.Warning, TestData.Now));
        notifications.Items.Add(Notification.Create(otherUserId, "Notificação de outro usuário.", NotificationType.Info, TestData.Now));
        var unitOfWork = new FakeUnitOfWork();
        var service = new NotificationService(notifications, new FakeClock(), unitOfWork);

        var result = await service.DeleteAllAsync(userId);

        Assert.True(result.IsSuccess);
        var remaining = Assert.Single(notifications.Items);
        Assert.Equal(otherUserId, remaining.UserId);
        Assert.Equal(1, unitOfWork.SaveChangesCalls);
    }
}
