namespace ToDoConsoleApp.Utils;

/// <summary>
/// Generic result wrapper for operations that can succeed or fail.
/// Provides a consistent way to handle operation results with error details.
/// </summary>
public class ResultWrapper<T>
{
    public bool IsSuccess { get; }
    public T? Data { get; }
    public string Message { get; }
    public string? ErrorDetails { get; }

    private ResultWrapper(bool isSuccess, T? data, string message, string? errorDetails)
    {
        IsSuccess = isSuccess;
        Data = data;
        Message = message;
        ErrorDetails = errorDetails;
    }

    public static ResultWrapper<T> Success(T data, string message = "Operation completed successfully")
        => new(true, data, message, null);

    public static ResultWrapper<T> Failure(string message, string? errorDetails = null)
        => new(false, default, message, errorDetails);

    public override string ToString()
    {
        if (IsSuccess)
            return $"✓ Success: {Message}";
        else
            return $"✗ Failure: {Message}{(ErrorDetails != null ? $" - {ErrorDetails}" : "")}";
    }
}