namespace PCRadio.Services.Interfaces;

public interface IFileDownloadService
{
    Task<bool> DownloadFileAsync(string url, string filePath);
}
