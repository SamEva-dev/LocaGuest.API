namespace LocaGuest.Application.Common;

/// <summary>
/// Generic result wrapper for operation outcomes
/// </summary>
public class Result
{
    public bool IsSuccess { get; }
    public string? ErrorMessage { get; }
    public bool IsFailure => !IsSuccess;

    protected Result(bool isSuccess, string? errorMessage)
    {
        IsSuccess = isSuccess;
        ErrorMessage = errorMessage;
    }

    public static Result Success() => new(true, null);
    public static Result Failure(string errorMessage) => new(false, errorMessage);
    public static Result<T> Success<T>(T data) => new(true, data, null);
    public static Result<T> Failure<T>(string errorMessage) => new(false, default, errorMessage);
}

/// <summary>
/// Generic result wrapper with data
/// </summary>
public class Result<T> : Result
{
    public T? Data { get; }

    internal Result(bool isSuccess, T? data, string? errorMessage)
        : base(isSuccess, errorMessage)
    {
        Data = data;
    }
}
