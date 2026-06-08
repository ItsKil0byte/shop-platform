namespace Order.Application.Interfaces;

public interface IModerationClient
{
    Task<bool> IsUserBannedAsync(string userId, CancellationToken cancellationToken = default);
}