namespace Gulp.Shared.Common;

/// <summary>
/// Represents the result of an operation that can succeed or fail
/// Follows the Result pattern to avoid exceptions for expected failures
/// </summary>
/// <typeparam name="T">The type of the success value</typeparam>
public class Result<T>
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public T? Value { get; }
    public string ErrorMessage { get; }
    public string? ErrorCode { get; }
    public Exception? Exception { get; }

    private Result(bool isSuccess, T? value, string errorMessage, string? errorCode = null, Exception? exception = null)
    {
        IsSuccess = isSuccess;
        Value = value;
        ErrorMessage = errorMessage;
        ErrorCode = errorCode;
        Exception = exception;
    }

    /// <summary>
    /// Creates a successful result
    /// </summary>
    public static Result<T> Success(T value) => new(true, value, string.Empty);

    /// <summary>
    /// Creates a failed result with an error message
    /// </summary>
    public static Result<T> Failure(string errorMessage, string? errorCode = null) => 
        new(false, default, errorMessage, errorCode);

    /// <summary>
    /// Creates a failed result with an exception (for unexpected errors)
    /// </summary>
    public static Result<T> Failure(Exception exception, string? userFriendlyMessage = null) => 
        new(false, default, userFriendlyMessage ?? "An unexpected error occurred", null, exception);

    /// <summary>
    /// Creates a failed result with error code and message
    /// </summary>
    public static Result<T> Failure(string errorMessage, string errorCode, Exception? exception = null) => 
        new(false, default, errorMessage, errorCode, exception);

    /// <summary>
    /// Implicit conversion from T to Result<T>
    /// </summary>
    public static implicit operator Result<T>(T value) => Success(value);

    /// <summary>
    /// Pattern matching support
    /// </summary>
    public void Match(Action<T> onSuccess, Action<string, string?, Exception?> onFailure)
    {
        if (IsSuccess)
            onSuccess(Value!);
        else
            onFailure(ErrorMessage, ErrorCode, Exception);
    }

    /// <summary>
    /// Pattern matching support with return value
    /// </summary>
    public TResult Match<TResult>(Func<T, TResult> onSuccess, Func<string, string?, Exception?, TResult> onFailure)
    {
        return IsSuccess ? onSuccess(Value!) : onFailure(ErrorMessage, ErrorCode, Exception);
    }
}

/// <summary>
/// Non-generic Result for operations that don't return a value
/// </summary>
public class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public string ErrorMessage { get; }
    public string? ErrorCode { get; }
    public Exception? Exception { get; }

    private Result(bool isSuccess, string errorMessage, string? errorCode = null, Exception? exception = null)
    {
        IsSuccess = isSuccess;
        ErrorMessage = errorMessage;
        ErrorCode = errorCode;
        Exception = exception;
    }

    /// <summary>
    /// Creates a successful result
    /// </summary>
    public static Result Success() => new(true, string.Empty);

    /// <summary>
    /// Creates a failed result with an error message
    /// </summary>
    public static Result Failure(string errorMessage, string? errorCode = null) => 
        new(false, errorMessage, errorCode);

    /// <summary>
    /// Creates a failed result with an exception
    /// </summary>
    public static Result Failure(Exception exception, string? userFriendlyMessage = null) => 
        new(false, userFriendlyMessage ?? "An unexpected error occurred", null, exception);

    /// <summary>
    /// Creates a failed result with error code and message
    /// </summary>
    public static Result Failure(string errorMessage, string errorCode, Exception? exception = null) => 
        new(false, errorMessage, errorCode, exception);

    /// <summary>
    /// Pattern matching support
    /// </summary>
    public void Match(Action onSuccess, Action<string, string?, Exception?> onFailure)
    {
        if (IsSuccess)
            onSuccess();
        else
            onFailure(ErrorMessage, ErrorCode, Exception);
    }

    /// <summary>
    /// Pattern matching support with return value
    /// </summary>
    public TResult Match<TResult>(Func<TResult> onSuccess, Func<string, string?, Exception?, TResult> onFailure)
    {
        return IsSuccess ? onSuccess() : onFailure(ErrorMessage, ErrorCode, Exception);
    }
}
