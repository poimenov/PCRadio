namespace PCRadio.Services.Interfaces;

public interface IMetadataService
{
    Task<MetadataResult> GetTitleAsync(string url);
}

public class MetadataResult
{
    public bool IsSuccess { get; }
    public string? Title { get; }
    public Exception? Error { get; }

    private MetadataResult(string? title, Exception? error, bool isSuccess)
    {
        Title = title;
        Error = error;
        IsSuccess = isSuccess;
    }

    public static MetadataResult Success(string? title) => new(title, null, true);
    public static MetadataResult Failure(Exception error) => new(null, error, false);

}