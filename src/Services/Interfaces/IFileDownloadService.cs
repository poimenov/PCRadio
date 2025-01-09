namespace PCRadio.Services.Interfaces;

public interface IFileDownloadService
{
    Task DownloadFileAsync(string url, string filePath);
}
