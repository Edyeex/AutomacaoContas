using AutoDownload.Application.Abstractions;
using AutoDownload.Application.Common;
using AutoDownload.Application.Contracts;
using AutoDownload.Application.Mappings;

namespace AutoDownload.Application.Services;

public sealed class NotificationService
{
    private readonly INotificationRepository notifications;
    private readonly IClock clock;
    private readonly IUnitOfWork unitOfWork;

    public NotificationService(INotificationRepository notifications, IClock clock, IUnitOfWork unitOfWork)
    {
        this.notifications = notifications;
        this.clock = clock;
        this.unitOfWork = unitOfWork;
    }

    public async Task<Result<IReadOnlyList<NotificationResponse>>> ListAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var list = await notifications.ListByUserAsync(userId, cancellationToken);
        return Result<IReadOnlyList<NotificationResponse>>.Success(list.Select(item => item.ToResponse()).ToList());
    }

    public async Task<Result> MarkAsReadAsync(Guid userId, Guid notificationId, bool read, CancellationToken cancellationToken = default)
    {
        var notification = await notifications.FindByIdForUserAsync(userId, notificationId, cancellationToken);
        if (notification is null)
        {
            return Result.Failure(Error.NotFound("notification.not_found", "Notification not found."));
        }

        if (read)
        {
            notification.MarkAsRead(clock.Now);
        }
        else
        {
            notification.MarkAsUnread();
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> MarkAllAsReadAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var list = await notifications.ListByUserAsync(userId, cancellationToken);
        foreach (var notification in list.Where(item => !item.IsRead))
        {
            notification.MarkAsRead(clock.Now);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> DeleteAsync(Guid userId, Guid notificationId, CancellationToken cancellationToken = default)
    {
        var notification = await notifications.FindByIdForUserAsync(userId, notificationId, cancellationToken);
        if (notification is null)
        {
            return Result.Failure(Error.NotFound("notification.not_found", "Notification not found."));
        }

        await notifications.RemoveAsync(notification, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> DeleteAllAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var list = await notifications.ListByUserAsync(userId, cancellationToken);
        foreach (var notification in list)
        {
            await notifications.RemoveAsync(notification, cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
