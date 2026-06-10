using Grpc.Core;
using Microsoft.Extensions.Logging;
using Moderation.Grpc;

namespace Moderation.Api.Grpc;

public class ModerationGrpcService(ILogger<ModerationGrpcService> logger) : ModerationService.ModerationServiceBase
{
    private readonly ILogger<ModerationGrpcService> _logger = logger;

    private static readonly HashSet<string> BannedUsers = new(StringComparer.OrdinalIgnoreCase)
    {
        "banned_user_123",
        "cheater",
        "spammer"
    };

    public override Task<CheckUserResponse> CheckUser(CheckUserRequest request, ServerCallContext context)
    {
        bool isBlocked = BannedUsers.Contains(request.UserId);

        _logger.LogInformation("Проверяем статус пользователя. Пользователь: {UserId}. Заблокирован: {IsBlocked}", request.UserId, isBlocked);

        return Task.FromResult(new CheckUserResponse
        {
            IsBlocked = isBlocked
        });
    }
}