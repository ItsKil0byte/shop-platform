using Grpc.Core;
using Moderation.Grpc;

namespace Moderation.Api.Grpc;

public class ModerationGrpcService : ModerationService.ModerationServiceBase
{
    private static readonly HashSet<string> BannedUsers = new(StringComparer.OrdinalIgnoreCase)
    {
        "banned_user_123",
        "cheater",
        "spammer"
    };

    public override Task<CheckUserResponse> CheckUser(CheckUserRequest request, ServerCallContext context)
    {
        bool isBlocked = BannedUsers.Contains(request.UserId);

        Console.WriteLine($"Проверка пользовать '{request.UserId}'. Результат: {isBlocked}");

        return Task.FromResult(new CheckUserResponse
        {
            IsBlocked = isBlocked
        });
    }
}