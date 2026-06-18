using Microsoft.Extensions.Logging;
using PCRadio.Services.Interfaces;

namespace PCRadio.Services;

public class FileDownloadService : IFileDownloadService
{
    public const string USER_AGENT_HEADER_NAME = "User-Agent";
    public const string USER_AGENT = "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36";

    private readonly ILogger<FileDownloadService> _logger;
    private readonly HttpClient _httpClient;

    public FileDownloadService(ILogger<FileDownloadService> logger, IHttpClientFactory httpClientFactory)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpClient = httpClientFactory.CreateClient();
    }

    public async Task<bool> DownloadFileAsync(string url, string filePath)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            _logger.LogWarning("DownloadFileAsync called with empty url");
            return false;
        }

        if (string.IsNullOrWhiteSpace(filePath))
        {
            _logger.LogWarning("DownloadFileAsync called with empty filePath");
            return false;
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            _logger.LogWarning("DownloadFileAsync called with unsupported URL scheme: {Url}", url);
            return false;
        }

        var tempFile = filePath + ".download";

        try
        {
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            using (var request = new HttpRequestMessage(HttpMethod.Get, url))
            {
                request.Headers.TryAddWithoutValidation(USER_AGENT_HEADER_NAME, USER_AGENT);

                using (var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead))
                {
                    if (!response.IsSuccessStatusCode)
                    {
                        _logger.LogWarning("Failed to download {Url}. Status code: {StatusCode}", url, response.StatusCode);
                        return false;
                    }

                    await using (var httpStream = await response.Content.ReadAsStreamAsync())
                    {
                        using (var fileStream = new FileStream(tempFile, FileMode.Create, FileAccess.Write, FileShare.None, 81920, FileOptions.Asynchronous))
                        {
                            await httpStream.CopyToAsync(fileStream);
                            await fileStream.FlushAsync();
                            fileStream.Close();
                        }
                    }
                }
            }

            if (File.Exists(filePath))
                File.Delete(filePath);

            File.Move(tempFile, filePath);

            _logger.LogInformation("File downloaded successfully: {Path}", filePath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading file {Url} to {Path}", url, filePath);
            try
            {
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }
            catch (Exception cleanupEx)
            {
                _logger.LogWarning(cleanupEx, "Failed to delete temp file {TempFile}", tempFile);
            }

            return false;
        }
    }
}
