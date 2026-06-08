using Grpc.Core;
using Moderation.Grpc;
using Order.Application.Interfaces;

namespace Order.Infrastructure.GrpcClients;

public class ModerationGrpcClient(ModerationService.ModerationServiceClient client) : IModerationClient
{
    private readonly ModerationService.ModerationServiceClient _client = client;

    public async Task<bool> IsUserBannedAsync(string userId, CancellationToken cancellationToken = default)
    {
        CheckUserRequest request = new() { UserId = userId };
        DateTime deadline = DateTime.UtcNow.AddSeconds(5);

        try
        {
            CheckUserResponse response = await _client.CheckUserAsync(
                request, deadline: deadline, cancellationToken: cancellationToken
            );
            
            return response.IsBlocked;
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.DeadlineExceeded)
        {
            throw new TimeoutException("Запрос к сервису модерации превысил время ожидания.", ex);
        }
    }
}