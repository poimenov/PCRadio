using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using PCRadio.Services.Interfaces;

namespace PCRadio.Services;

public class SearchService : ISearchService
{
    private const string BASE_URL = "https://m.z3.fm";
    private readonly string _downloadPath;
    private readonly string _baseUrl;
    private readonly HttpClient _httpClient;
    public SearchService(IOptions<AppSettings> options, IHttpClientFactory httpClientFactory)
    {
        var configPath = options.Value.DownloadPath;
        if (string.IsNullOrWhiteSpace(configPath) || !Directory.Exists(configPath))
        {
            _downloadPath = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
        }
        else
        {
            _downloadPath = configPath;
        }
        Directory.CreateDirectory(_downloadPath);

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
        var request = new HttpRequestMessage(HttpMethod.Get, downloadUrl);
        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Failed to download file. Status code: {response.StatusCode}");
        }

        var fileName = response.Content.Headers.ContentDisposition?.FileName
                       ?? response.Content.Headers.ContentDisposition?.FileNameStar?.ToString();

        if (string.IsNullOrWhiteSpace(fileName))
        {
            fileName = track.FileName;
        }

        var filePath = Path.Combine(_downloadPath, fileName);

        using var networkStream = await response.Content.ReadAsStreamAsync();
        using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true);

        await networkStream.CopyToAsync(fileStream);
    }

    public async Task<IEnumerable<Track>> Search(string keyword)
    {
        var escapedKeyword = Uri.EscapeDataString(keyword);
        var result = await _httpClient.GetFromJsonAsync<IEnumerable<Track>>($"mp3/search?keywords={escapedKeyword}", default);
        return result ?? Enumerable.Empty<Track>();
    }

}