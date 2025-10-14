using System.Diagnostics;
using Microsoft.Extensions.Logging;
using PCRadio.Services.Interfaces;

namespace PCRadio.Services;

public class FileDownloadService : IFileDownloadService
{
    public const string USER_AGENT_HEADER_NAME = "User-Agent";
    public const string USER_AGENT = "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36";
    ILogger<FileDownloadService> _logger;
    private readonly HttpClient _httpClient;
    public FileDownloadService(ILogger<FileDownloadService> logger, HttpClient httpClient)
    {
        _logger = logger;
        _httpClient = httpClient;
    }

    public async Task DownloadFileAsync(string url, string filePath)
    {
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add(USER_AGENT_HEADER_NAME, USER_AGENT);
        HttpResponseMessage response = await _httpClient.GetAsync(url);
        if (response.IsSuccessStatusCode)
        {
            using (Stream stream = await response.Content.ReadAsStreamAsync())
            {
                using (FileStream fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await stream.CopyToAsync(fileStream);
                }
                _logger.LogInformation("File downloaded successfully.");
                Debug.WriteLine("File downloaded successfully.");
            }
        }
    }
}
