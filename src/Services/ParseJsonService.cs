using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PCRadio.Services.Interfaces;

namespace PCRadio.Services;

public class ParseJsonService : IParseJsonService
{
    private readonly ILogger<ParseJsonService> _logger;

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
            var obj = new JsonSerializer().Deserialize<Dictionary<string, int[]>>(reader);
            if (obj == null || obj.Count == 0)
                return new Dictionary<int, int[]>();

            return obj.ToDictionary(x =>
            {
                if (!int.TryParse(x.Key, out var key) || key <= 0 || x.Value is null)
                {
                    throw new InvalidDataException($"Invalid relation entry '{x.Key}'.");
                }

                return key;
            }, x => x.Value);
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
            var obj = new JsonSerializer().Deserialize<List<Item>>(reader);
            if (obj == null || obj.Count == 0)
                return new Dictionary<int, string>();

            if (obj.Any(x => x.Id <= 0 || string.IsNullOrWhiteSpace(x.Name)))
            {
                throw new InvalidDataException("Dictionary entries must have positive IDs and non-empty names.");
            }

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
            var obj = new JsonSerializer().Deserialize<List<ParseStation>>(reader);
            if (obj == null || obj.Count == 0)
                return new Dictionary<int, ParseStation>();

            if (obj.Any(x => x.Id <= 0))
            {
                throw new InvalidDataException("Station IDs must be positive.");
            }

            return obj.ToDictionary(x => x.Id, x => x);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to deserialize stations from JSON", ex);
        }
    }

    private static void MarkSection(HashSet<string> parsedSections, string sectionName)
    {
        if (!parsedSections.Add(sectionName))
        {
            throw new InvalidDataException($"JSON contains duplicate section '{sectionName}'.");
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
        var parsedSections = new HashSet<string>(StringComparer.Ordinal);

        try
        {
            _logger.LogInformation("ParseJsonService.ParseAsync started for file: {Path}", jsonFilePath);

            using var streamReader = new StreamReader(jsonFilePath);
            await using var reader = new JsonTextReader(streamReader);

            while (await reader.ReadAsync())
            {
                if (reader.TokenType == JsonToken.StartObject && reader.Path == "countries_cities")
                {
                    MarkSection(parsedSections, "countries_cities");
                    retVal.CountriesCities = GetRelations(reader);
                    _logger.LogDebug("Parsed countries_cities: {Count} entries", retVal.CountriesCities.Count);
                }

                if (reader.TokenType == JsonToken.StartObject && reader.Path == "genres_subgenres")
                {
                    MarkSection(parsedSections, "genres_subgenres");
                    retVal.GenresSubgenres = GetRelations(reader);
                    _logger.LogDebug("Parsed genres_subgenres: {Count} entries", retVal.GenresSubgenres.Count);
                }

                if (reader.TokenType == JsonToken.StartArray && reader.Path == "countries")
                {
                    MarkSection(parsedSections, "countries");
                    retVal.Countries = GetDictionary(reader);
                    _logger.LogDebug("Parsed countries: {Count} entries", retVal.Countries.Count);
                }

                if (reader.TokenType == JsonToken.StartArray && reader.Path == "cities")
                {
                    MarkSection(parsedSections, "cities");
                    retVal.Cities = GetDictionary(reader);
                    _logger.LogDebug("Parsed cities: {Count} entries", retVal.Cities.Count);
                }

                if (reader.TokenType == JsonToken.StartArray && reader.Path == "genres")
                {
                    MarkSection(parsedSections, "genres");
                    retVal.Genres = GetDictionary(reader);
                    _logger.LogDebug("Parsed genres: {Count} entries", retVal.Genres.Count);
                }

                if (reader.TokenType == JsonToken.StartArray && reader.Path == "subgenres")
                {
                    MarkSection(parsedSections, "subgenres");
                    retVal.Subgenres = GetDictionary(reader);
                    _logger.LogDebug("Parsed subgenres: {Count} entries", retVal.Subgenres.Count);
                }

                if (reader.TokenType == JsonToken.StartArray && reader.Path == "stations")
                {
                    MarkSection(parsedSections, "stations");
                    retVal.Stations = GetStations(reader);
                    _logger.LogDebug("Parsed stations: {Count} entries", retVal.Stations.Count);
                }
            }

            var requiredSections = new[]
            {
                "countries_cities", "genres_subgenres", "countries", "cities", "genres", "subgenres", "stations"
            };
            var missingSections = requiredSections.Where(section => !parsedSections.Contains(section)).ToArray();
            if (missingSections.Length > 0)
            {
                throw new InvalidDataException(
                    $"JSON is missing required sections: {string.Join(", ", missingSections)}.");
            }

            if (retVal.Countries.Count == 0 || retVal.Genres.Count == 0 || retVal.Stations.Count == 0)
            {
                throw new InvalidDataException("JSON must contain countries, genres, and stations.");
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
