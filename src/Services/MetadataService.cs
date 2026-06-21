using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PCRadio.Services.Interfaces;
namespace PCRadio.Services;

public class MetadataService : IMetadataService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AppSettings _settings;
    private readonly ILogger<MetadataService> _logger;

    private static readonly Regex StreamTitleRegex = new(
        "StreamTitle='([^']*)'",
        RegexOptions.Compiled);

    private static readonly Regex AttributeRegex = new(
        "(\\w+)=\"([^\"]*)\"",
        RegexOptions.Compiled);

    public MetadataService(
        IHttpClientFactory httpClientFactory,
        IOptions<AppSettings> options,
        ILogger<MetadataService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _settings = options.Value;
        _logger = logger;
    }

    public async Task<MetadataResult> GetTitleAsync(string url)
    {
        try
        {
            using var client = CreateHttpClient();
            using var request = CreateRequest(url);
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

            response.EnsureSuccessStatusCode();

            var metaInt = GetMetaInt(response);
            if (metaInt <= 0)
            {
                return MetadataResult.Success(null);
            }

            var title = await ReadMetadataFromStream(response, metaInt);
            return MetadataResult.Success(title);
        }
        catch (HttpRequestException ex)
        {
            return MetadataResult.Failure(ex);
        }
        catch (OperationCanceledException ex)
        {
            return MetadataResult.Failure(ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while getting metadata from {Url}", url);
            return MetadataResult.Failure(ex);
        }
    }

    private HttpClient CreateHttpClient()
    {
        var client = _httpClientFactory.CreateClient("PCRadio.MetadataService.Client");

        var timeoutMs = (_settings.TitleDelayMilliseconds < 4000) ? 4000 : _settings.TitleDelayMilliseconds - 1000;
        if (client.Timeout > TimeSpan.FromMilliseconds(timeoutMs))
        {
            client.Timeout = TimeSpan.FromMilliseconds(timeoutMs);
        }

        return client;
    }

    private static HttpRequestMessage CreateRequest(string url)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.TryAddWithoutValidation("Icy-MetaData", "1");
        return request;
    }

    private static int GetMetaInt(HttpResponseMessage response)
    {
        var headerValue = TryGetHeader(response, "icy-metaint");
        return int.TryParse(headerValue, out var value) ? value : 0;
    }

    private static string? TryGetHeader(HttpResponseMessage response, string name)
    {
        if (response.Headers.TryGetValues(name, out var values))
        {
            return GetFirstValue(values);
        }

        if (response.Content.Headers.TryGetValues(name, out var contentValues))
        {
            return GetFirstValue(contentValues);
        }

        return null;
    }

    private static string? GetFirstValue(IEnumerable<string> values)
    {
        using var enumerator = values.GetEnumerator();
        return enumerator.MoveNext() ? enumerator.Current : null;
    }

    private static async Task<string?> ReadMetadataFromStream(HttpResponseMessage response, int metaInt)
    {
        await using var stream = await response.Content.ReadAsStreamAsync();

        // Skip audio data before metadata
        var buffer = new byte[metaInt];
        await stream.ReadExactlyAsync(buffer, 0, buffer.Length);

        // Read metadata length
        var lengthByte = stream.ReadByte();
        if (lengthByte < 0)
        {
            return null;
        }

        var metadataLength = lengthByte * 16;
        if (metadataLength <= 0)
        {
            return null;
        }

        // Read and parse metadata
        var metadataBuffer = new byte[metadataLength];
        var bytesRead = await stream.ReadAsync(metadataBuffer, 0, metadataLength);
        var metadata = Encoding.UTF8.GetString(metadataBuffer, 0, bytesRead).TrimEnd('\0');

        return ExtractTitleFromMetadata(metadata);
    }

    private static string? ExtractTitleFromMetadata(string metadata)
    {
        var match = StreamTitleRegex.Match(metadata);
        if (!match.Success)
        {
            return null;
        }

        var raw = match.Groups[1].Value.Trim();
        var attributeMatches = AttributeRegex.Matches(raw);

        // Simple case: "Artist - Track" without additional attributes
        if (attributeMatches.Count == 0)
        {
            return TrimTrailingDash(raw);
        }

        // Complex case: contains attributes like key="value"
        return ExtractTitleFromComplexMetadata(raw, attributeMatches);
    }

    private static string? ExtractTitleFromComplexMetadata(string raw, MatchCollection attributeMatches)
    {
        var firstAttribute = attributeMatches[0];
        var firstAttrIndex = raw.IndexOf(firstAttribute.Value, StringComparison.Ordinal);

        var mainTitle = firstAttrIndex > 0
            ? TrimTrailingDash(raw[..firstAttrIndex].Trim())
            : string.Empty;

        // Build attribute dictionary
        var attributes = new Dictionary<string, string>();
        foreach (Match attrMatch in attributeMatches)
        {
            attributes[attrMatch.Groups[1].Value] = attrMatch.Groups[2].Value;
        }

        // Combine title with text attribute if present
        var title = attributes.TryGetValue("text", out var textAttr) && !string.IsNullOrWhiteSpace(textAttr)
            ? string.IsNullOrWhiteSpace(mainTitle)
                ? textAttr
                : $"{mainTitle} - {textAttr}"
            : mainTitle;

        return string.IsNullOrWhiteSpace(title) ? null : title;
    }

    private static string TrimTrailingDash(string value)
    {
        return value.EndsWith("-")
            ? value[..^1].Trim()
            : value;
    }
}