using System.Globalization;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PCRadio.DataAccess.Interfaces;
using PCRadio.DataAccess.Models;
using PCRadio.Services.Interfaces;
using PCRadio.Services.Models;

namespace PCRadio.Services;

public sealed class StationTopicsService : IStationTopicsService
{
    private readonly HttpClient _httpClient;
    private readonly Uri _apiBaseUri;
    private readonly IStations _stations;
    private readonly ICountries _countries;
    private readonly IGenres _genres;
    private readonly ILogger<StationTopicsService> _logger;
    private readonly int _pageSize;

    public StationTopicsService(
        IHttpClientFactory httpClientFactory,
        IOptions<AppSettings> options,
        IStations stations,
        ICountries countries,
        IGenres genres,
        ILogger<StationTopicsService> logger)
    {
        _httpClient = httpClientFactory.CreateClient("DefaultHttpClient");
        _apiBaseUri = CreateApiBaseUri(options.Value.StationTopicsApiBaseUrl);
        _pageSize = options.Value.PageSize > 0 ? options.Value.PageSize : 20;
        _stations = stations;
        _countries = countries;
        _genres = genres;
        _logger = logger;
    }

    private static Uri CreateApiBaseUri(string? baseUrl)
    {
        if (string.IsNullOrWhiteSpace(baseUrl)
            || !Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri)
            || uri.Scheme != Uri.UriSchemeHttps
            || !string.IsNullOrEmpty(uri.UserInfo)
            || !string.IsNullOrEmpty(uri.Query)
            || !string.IsNullOrEmpty(uri.Fragment))
        {
            throw new InvalidOperationException(
                "StationTopicsApiBaseUrl must be an absolute HTTPS URL without user info, query, or fragment.");
        }

        return new Uri(uri.AbsoluteUri.TrimEnd('/') + "/", UriKind.Absolute);
    }

    public async Task<IReadOnlyList<StationTopic>> GetTopicsAsync()
    {
        var language = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        var response = await _httpClient.GetFromJsonAsync<TopicsResponse>(
            new Uri(_apiBaseUri, $"station-topics?lang={Uri.EscapeDataString(language)}"));

        return response?.Results
            ?? throw new InvalidDataException("The station topics response did not contain a results list.");
    }

    public async Task<StationTopic?> GetTopicBySlugAsync(string slug)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(slug);
        var topics = await GetTopicsAsync();
        return topics.FirstOrDefault(topic => string.Equals(topic.Slug, slug, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<StationTopicStationsResult> GetByTopicAsync(string slug, int skip, int take)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(slug);
        ArgumentOutOfRangeException.ThrowIfNegative(skip);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(take);

        var page = skip / take + 1;
        var language = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        var url = new Uri(_apiBaseUri, $"station-topics/{Uri.EscapeDataString(slug)}/stations/"
            + $"?page={page}&page_size={take}&sort=popular&lang={Uri.EscapeDataString(language)}");
        var response = await _httpClient.GetFromJsonAsync<TopicStationsResponse>(url)
            ?? throw new InvalidDataException("The station topic response was empty.");
        var apiStations = response.Results
            ?? throw new InvalidDataException("The station topic response did not contain a results list.");

        var countriesByName = _countries.GetAll()
            .GroupBy(country => NormalizeName(country.Name), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
        var countryIdsByCode = AppCultures.CountryCodes
            .ToDictionary(item => item.Value, item => item.Key, StringComparer.OrdinalIgnoreCase);
        var genres = _genres.GetAll().ToList();
        var subGenres = _genres.GetAllSubGenres().ToList();
        var stations = new List<Station>(apiStations.Count);

        foreach (var apiStation in apiStations)
        {
            var stream = apiStation.Streams?
                .Select(item => item.Url?.Trim() ?? string.Empty)
                .FirstOrDefault(url => !string.IsNullOrWhiteSpace(url));
            if (string.IsNullOrWhiteSpace(stream))
            {
                throw new InvalidDataException($"Station '{apiStation.Name}' does not have a usable stream URL.");
            }

            var existingStation = await _stations.GetByStreamAsync(stream);
            if (existingStation is not null)
            {
                stations.Add(existingStation);
                continue;
            }

            var station = new Station
            {
                Name = apiStation.Name,
                Stream = stream,
                Logo = apiStation.LogoUrl ?? string.Empty,
                Description = apiStation.Description,
                CountryId = GetCountryId(apiStation, countriesByName, countryIdsByCode),
                StationGenres = [],
                StationSubGenres = []
            };

            foreach (var genreName in (apiStation.Genres ?? [])
                         .Select(genre => NormalizeName(genre.Name))
                         .Distinct(StringComparer.OrdinalIgnoreCase))
            {
                var genre = genres.FirstOrDefault(item =>
                    string.Equals(NormalizeName(item.Name), genreName, StringComparison.OrdinalIgnoreCase));
                if (genre is not null)
                {
                    station.StationGenres.Add(new StationGenre
                    {
                        Station = station,
                        Genre = genre,
                        GenreId = genre.Id
                    });
                }

                var subGenre = subGenres.FirstOrDefault(item =>
                    string.Equals(NormalizeName(item.Name), genreName, StringComparison.OrdinalIgnoreCase));
                if (subGenre is not null)
                {
                    station.StationSubGenres.Add(new StationSubGenre
                    {
                        Station = station,
                        SubGenre = subGenre,
                        SubGenreId = subGenre.Id
                    });
                }
            }

            var addResult = await _stations.AddStationAsync(station);
            if (!addResult.Success || addResult.Result is null)
            {
                throw new InvalidOperationException(
                    $"Failed to add station '{apiStation.Name}' from topic '{slug}': {addResult.Message}",
                    addResult.Exception);
            }

            stations.Add(addResult.Result);
        }

        _logger.LogDebug(
            "Loaded {StationCount} stations for topic {TopicSlug}, page {Page} of {TotalCount}",
            stations.Count,
            slug,
            page,
            response.Count);

        return new StationTopicStationsResult
        {
            Stations = stations,
            TotalCount = response.Count,
            TopicName = response.Topic?.Name ?? slug
        };
    }

    public async Task<IEnumerable<StationInfo>> ExportByTopicAsync(string slug)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(slug);

        var firstPage = await GetByTopicAsync(slug, 0, _pageSize);
        var stationIds = firstPage.Stations.Select(station => station.Id).ToList();

        while (stationIds.Count < firstPage.TotalCount)
        {
            var nextPage = await GetByTopicAsync(slug, stationIds.Count, _pageSize);
            var nextPageStationIds = nextPage.Stations.Select(station => station.Id).ToList();
            if (nextPageStationIds.Count == 0)
            {
                throw new InvalidDataException(
                    $"Topic '{slug}' returned no stations before all {firstPage.TotalCount} stations were loaded.");
            }

            stationIds.AddRange(nextPageStationIds);
        }

        return await _stations.ExportByIdsAsync(stationIds);
    }

    private static string NormalizeName(string? name) => name?.Trim() ?? string.Empty;

    private static int? GetCountryId(
        TopicStation station,
        IReadOnlyDictionary<string, Country> countriesByName,
        IReadOnlyDictionary<string, int> countryIdsByCode)
    {
        var country = station.Country;
        if (country is null)
        {
            throw new InvalidDataException($"Station '{station.Name}' does not have country information.");
        }

        if (countriesByName.TryGetValue(NormalizeName(country.Name), out var matchedCountry))
        {
            return matchedCountry.Id;
        }

        if (!string.IsNullOrWhiteSpace(country.Code)
            && countryIdsByCode.TryGetValue(country.Code, out var countryId))
        {
            return countryId;
        }

        throw new InvalidDataException(
            $"Country '{country.Name}' ({country.Code}) for station '{station.Name}' was not found in the local database.");
    }

    private sealed class TopicsResponse
    {
        public TopicsResponse() { }

        [System.Text.Json.Serialization.JsonPropertyName("results")]
        public List<StationTopic>? Results { get; set; }
    }

    private sealed class TopicStationsResponse
    {
        public TopicStationsResponse() { }

        [System.Text.Json.Serialization.JsonPropertyName("topic")]
        public StationTopic? Topic { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("count")]
        public int Count { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("results")]
        public List<TopicStation>? Results { get; set; }
    }

    private sealed class TopicStation
    {
        public TopicStation() { }

        [System.Text.Json.Serialization.JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [System.Text.Json.Serialization.JsonPropertyName("description")]
        public string? Description { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("logo_url")]
        public string? LogoUrl { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("streams")]
        public List<TopicStream>? Streams { get; set; } = [];

        [System.Text.Json.Serialization.JsonPropertyName("country")]
        public TopicCountry? Country { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("genres")]
        public List<TopicGenre>? Genres { get; set; } = [];
    }

    private sealed class TopicStream
    {
        public TopicStream() { }

        [System.Text.Json.Serialization.JsonPropertyName("url")]
        public string Url { get; set; } = string.Empty;
    }

    private sealed class TopicCountry
    {
        public TopicCountry() { }

        [System.Text.Json.Serialization.JsonPropertyName("code")]
        public string? Code { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
    }

    private sealed class TopicGenre
    {
        public TopicGenre() { }

        [System.Text.Json.Serialization.JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
    }
}
