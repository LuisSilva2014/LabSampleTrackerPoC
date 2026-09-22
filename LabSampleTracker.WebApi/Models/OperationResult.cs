namespace LabSampleTracker.WebApi.Models;

/// <summary>
/// The outcome of a service call. The controller turns this into HTTP 200 or HTTP 400.
/// </summary>
public class OperationResult<T>
{
    public bool Succeeded { get; private init; }

    public string? Error { get; private init; }

    public T? Value { get; private init; }

    public static OperationResult<T> Ok(T value)
    {
        return new OperationResult<T>
        {
            Succeeded = true,
            Value = value
        };
    }

    public static OperationResult<T> Fail(string error)
    {
        return new OperationResult<T>
        {
            Succeeded = false,
            Error = error
        };
    }
}
