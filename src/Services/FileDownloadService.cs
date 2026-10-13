using Microsoft.Extensions.Logging;
using PCRadio.Services.Interfaces;

namespace PCRadio.Services;

public class FileDownloadService : IFileDownloadService
{
    private const long MAX_DOWNLOAD_SIZE_BYTES = 25L * 1024 * 1024;
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

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
        {
            _logger.LogWarning("DownloadFileAsync requires an HTTPS URL: {Url}", url);
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

                    if (response.RequestMessage?.RequestUri?.Scheme != Uri.UriSchemeHttps)
                    {
                        throw new InvalidDataException("The download response did not originate from an HTTPS URL.");
                    }

                    if (response.Content.Headers.ContentLength is > MAX_DOWNLOAD_SIZE_BYTES)
                    {
                        throw new InvalidDataException(
                            $"Download exceeds the maximum allowed size of {MAX_DOWNLOAD_SIZE_BYTES} bytes.");
                    }

                    await using (var httpStream = await response.Content.ReadAsStreamAsync())
                    {
                        using (var fileStream = new FileStream(tempFile, FileMode.Create, FileAccess.Write, FileShare.None, 81920, FileOptions.Asynchronous))
                        {
                            var buffer = new byte[81920];
                            long totalBytesRead = 0;
                            int bytesRead;
                            while ((bytesRead = await httpStream.ReadAsync(buffer)) > 0)
                            {
                                totalBytesRead += bytesRead;
                                if (totalBytesRead > MAX_DOWNLOAD_SIZE_BYTES)
                                {
                                    throw new InvalidDataException(
                                        $"Download exceeds the maximum allowed size of {MAX_DOWNLOAD_SIZE_BYTES} bytes.");
                                }

                                await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead));
                            }

                            await fileStream.FlushAsync();
                        }
                    }
                }
            }

            File.Move(tempFile, filePath, overwrite: true);

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
