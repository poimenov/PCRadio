namespace PCRadio.Common;

public class OperationResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public Exception? Exception { get; set; }
}

public class OperationResult<T> : OperationResult
{
    public T? Result { get; set; }
}