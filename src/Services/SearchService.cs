using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using PCRadio.Services.Interfaces;

namespace PCRadio.Services;

public class SearchService : ISearchService
{
    private const string BASE_URL = "https://m.z3.fm";
    private readonly IOptions<AppSettings> _options;
    private readonly string _baseUrl;
    private readonly HttpClient _httpClient;
    public SearchService(IOptions<AppSettings> options, IHttpClientFactory httpClientFactory)
    {
        _options = options;

        _baseUrl = options.Value.TrackDownloadBaseUrl;
        if (string.IsNullOrWhiteSpace(_baseUrl) || !Uri.IsWellFormedUriString(_baseUrl, UriKind.Absolute))
        {
            _baseUrl = BASE_URL;
        }
        _httpClient = httpClientFactory.CreateClient("PCRadio.SearchService.Client");
        _httpClient.BaseAddress = new Uri(_baseUrl);
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        _httpClient.DefaultRequestHeaders.Add("x-requested-with", "XMLHttpRequest");
    }

    public async Task Download(Track track)
    {
        var downloadUrl = $"{_httpClient.BaseAddress}download/{track.Id}";
        using var request = new HttpRequestMessage(HttpMethod.Get, downloadUrl);
        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Failed to download file. Status code: {response.StatusCode}");
        }

        var fileName = response.Content.Headers.ContentDisposition?.FileNameStar?.ToString()
                       ?? response.Content.Headers.ContentDisposition?.FileName;

        if (string.IsNullOrWhiteSpace(fileName))
        {
            fileName = track.FileName;
        }

        fileName = GetSafeFileName(fileName);

        var downloadDirectory = Path.GetFullPath(GetDownloadPath());
        Directory.CreateDirectory(downloadDirectory);
        var filePath = Path.GetFullPath(Path.Combine(downloadDirectory, fileName));
        var relativePath = Path.GetRelativePath(downloadDirectory, filePath);
        if (Path.IsPathRooted(relativePath)
            || relativePath == ".."
            || relativePath.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
        {
            throw new InvalidDataException("The download file path must remain inside the download directory.");
        }

        var temporaryPath = Path.Combine(downloadDirectory, $".{Guid.NewGuid():N}.download");
        try
        {
            await using (var networkStream = await response.Content.ReadAsStreamAsync())
            await using (var fileStream = new FileStream(
                             temporaryPath,
                             FileMode.CreateNew,
                             FileAccess.Write,
                             FileShare.None,
                             4096,
                             useAsync: true))
            {
                await networkStream.CopyToAsync(fileStream);
            }

            File.Move(temporaryPath, filePath);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    private static string GetSafeFileName(string fileName)
    {
        var safeFileName = Path.GetFileName(fileName.Trim().Trim('"').Replace('\\', '/'));
        if (string.IsNullOrWhiteSpace(safeFileName)
            || safeFileName is "." or ".."
            || safeFileName.EndsWith('.')
            || safeFileName.EndsWith(' ')
            || safeFileName.Any(character => char.IsControl(character) || "<>:\"|?*".Contains(character)))
        {
            throw new InvalidDataException("The download response did not provide a valid file name.");
        }

        var deviceName = safeFileName.Split('.')[0].TrimEnd(' ');
        if (deviceName.Equals("CON", StringComparison.OrdinalIgnoreCase)
            || deviceName.Equals("PRN", StringComparison.OrdinalIgnoreCase)
            || deviceName.Equals("AUX", StringComparison.OrdinalIgnoreCase)
            || deviceName.Equals("NUL", StringComparison.OrdinalIgnoreCase)
            || (deviceName.Length == 4
                && (deviceName.StartsWith("COM", StringComparison.OrdinalIgnoreCase)
                    || deviceName.StartsWith("LPT", StringComparison.OrdinalIgnoreCase))
                && deviceName[3] is >= '1' and <= '9'))
        {
            throw new InvalidDataException("The download response provided a reserved file name.");
        }

        return safeFileName;
    }

    public async Task<IEnumerable<Track>> Search(string keyword)
    {
        var escapedKeyword = Uri.EscapeDataString(keyword);
        var result = await _httpClient.GetFromJsonAsync<IEnumerable<Track>>($"mp3/search?keywords={escapedKeyword}", default);
        return result ?? Enumerable.Empty<Track>();
    }

    private string GetDownloadPath()
    {
        var configuredPath = _options.Value.DownloadPath;
        return !string.IsNullOrWhiteSpace(configuredPath)
            ? configuredPath
            : Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
    }

}