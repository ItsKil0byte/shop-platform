namespace Auth.Application.Results;

public sealed record ServiceResult<T>(T? Value, AuthError? Error, string? Message)
{
    public bool IsSuccess => Error is null;

    public static ServiceResult<T> Success(T value)
    {
        return new ServiceResult<T>(value, null, null);
    }

    public static ServiceResult<T> Failure(AuthError error, string message)
    {
        return new ServiceResult<T>(default, error, message);
    }
}

