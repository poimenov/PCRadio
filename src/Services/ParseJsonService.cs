using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PCRadio.Services.Interfaces;

namespace PCRadio.Services;

public class ParseJsonService : IParseJsonService
{
    private readonly ILogger<ParseJsonService> _logger;
    private static readonly JsonSerializer Serializer = new();

    public ParseJsonService(ILogger<ParseJsonService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    private class Item
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;

        public override string ToString()
        {
            return $"Id: {Id}, Name: {Name}";
        }
    }

    private static Dictionary<int, int[]> GetRelations(JsonTextReader reader)
    {
        try
        {
            var obj = Serializer.Deserialize<Dictionary<string, int[]>>(reader);
            if (obj == null || obj.Count == 0)
                return new Dictionary<int, int[]>();

            return obj.ToDictionary(
                x => int.TryParse(x.Key, out var key) ? key : 0,
                x => x.Value ?? Array.Empty<int>());
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to deserialize relations from JSON", ex);
        }
    }

    private static Dictionary<int, string> GetDictionary(JsonTextReader reader)
    {
        try
        {
            var obj = Serializer.Deserialize<List<Item>>(reader);
            if (obj == null || obj.Count == 0)
                return new Dictionary<int, string>();

            return obj.ToDictionary(x => x.Id, x => x.Name);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to deserialize dictionary from JSON", ex);
        }
    }

    private static Dictionary<int, ParseStation> GetStations(JsonTextReader reader)
    {
        try
        {
            var obj = Serializer.Deserialize<List<ParseStation>>(reader);
            if (obj == null || obj.Count == 0)
                return new Dictionary<int, ParseStation>();

            return obj.ToDictionary(x => x.Id, x => x);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to deserialize stations from JSON", ex);
        }
    }

    public async Task<ParseResult> ParseAsync(string jsonFilePath)
    {
        if (string.IsNullOrWhiteSpace(jsonFilePath))
        {
            _logger.LogError("ParseAsync called with empty or null jsonFilePath");
            throw new ArgumentException("jsonFilePath must not be null or empty", nameof(jsonFilePath));
        }

        if (!File.Exists(jsonFilePath))
        {
            _logger.LogError("JSON file not found: {Path}", jsonFilePath);
            throw new FileNotFoundException($"JSON file not found: {jsonFilePath}", jsonFilePath);
        }

        var retVal = new ParseResult();

        try
        {
            _logger.LogInformation("ParseJsonService.ParseAsync started for file: {Path}", jsonFilePath);

            using var streamReader = new StreamReader(jsonFilePath);
            await using var reader = new JsonTextReader(streamReader);
            reader.SupportMultipleContent = true;

            while (await reader.ReadAsync())
            {
                try
                {
                    if (reader.TokenType == JsonToken.StartObject && reader.Path == "countries_cities")
                    {
                        retVal.CountriesCities = GetRelations(reader);
                        _logger.LogDebug("Parsed countries_cities: {Count} entries", retVal.CountriesCities.Count);
                    }

                    if (reader.TokenType == JsonToken.StartObject && reader.Path == "genres_subgenres")
                    {
                        retVal.GenresSubgenres = GetRelations(reader);
                        _logger.LogDebug("Parsed genres_subgenres: {Count} entries", retVal.GenresSubgenres.Count);
                    }

                    if (reader.TokenType == JsonToken.StartArray && reader.Path == "countries")
                    {
                        retVal.Countries = GetDictionary(reader);
                        _logger.LogDebug("Parsed countries: {Count} entries", retVal.Countries.Count);
                    }

                    if (reader.TokenType == JsonToken.StartArray && reader.Path == "cities")
                    {
                        retVal.Cities = GetDictionary(reader);
                        _logger.LogDebug("Parsed cities: {Count} entries", retVal.Cities.Count);
                    }

                    if (reader.TokenType == JsonToken.StartArray && reader.Path == "genres")
                    {
                        retVal.Genres = GetDictionary(reader);
                        _logger.LogDebug("Parsed genres: {Count} entries", retVal.Genres.Count);
                    }

                    if (reader.TokenType == JsonToken.StartArray && reader.Path == "subgenres")
                    {
                        retVal.Subgenres = GetDictionary(reader);
                        _logger.LogDebug("Parsed subgenres: {Count} entries", retVal.Subgenres.Count);
                    }

                    if (reader.TokenType == JsonToken.StartArray && reader.Path == "stations")
                    {
                        retVal.Stations = GetStations(reader);
                        _logger.LogDebug("Parsed stations: {Count} entries", retVal.Stations.Count);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error parsing JSON element at path: {Path}", reader.Path);
                }
            }

            _logger.LogInformation("File parsed successfully. Station count: {Count}", retVal.Stations.Count);
            return retVal;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing JSON file: {Path}", jsonFilePath);
            throw;
        }
    }
}
