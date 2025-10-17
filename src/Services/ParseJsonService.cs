using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PCRadio.Services.Interfaces;

namespace PCRadio.Services;

public class ParseJsonService : IParseJsonService
{
    private readonly ILogger<ParseJsonService> _logger;
    public ParseJsonService(ILogger<ParseJsonService> logger)
    {
        _logger = logger;
    }

    class Item
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public override string ToString()
        {
            return $"Id: {Id}, Name: {Name}";
        }
    }

    static Dictionary<int, int[]> getRelations(JsonTextReader reader)
    {
        var serializer = new JsonSerializer();
        var obj = serializer.Deserialize<Dictionary<string, int[]>>(reader);
        if (obj != null)
        {
            var dict = obj;
            return dict.ToDictionary(x => Convert.ToInt32(x.Key), x => x.Value);
        }

        return new Dictionary<int, int[]>();
    }

    static Dictionary<int, string> getDictionary(JsonTextReader reader)
    {
        var serializer = new JsonSerializer();
        var obj = serializer.Deserialize<List<Item>>(reader);
        if (obj != null)
        {
            var dict = obj;
            return dict.ToDictionary(x => Convert.ToInt32(x.Id), x => x.Name);
        }

        return new Dictionary<int, string>();
    }

    static Dictionary<int, ParseStation> getStations(JsonTextReader reader)
    {
        var serializer = new JsonSerializer();
        var obj = serializer.Deserialize<List<ParseStation>>(reader);
        if (obj != null)
        {
            var dict = obj;
            return dict.ToDictionary(x => Convert.ToInt32(x.Id), x => x);
        }

        return new Dictionary<int, ParseStation>();
    }
    public async Task<ParseResult> ParseAsync(string jsonFilePath)
    {
        var retVal = new ParseResult();
        Debug.WriteLine("ParseJsonService.Parse started");
        using var streamReader = new StreamReader(jsonFilePath);
        await using var reader = new JsonTextReader(streamReader);
        reader.SupportMultipleContent = true;
        while (await reader.ReadAsync())
        {
            if (reader.TokenType == JsonToken.StartObject && reader.Path == "countries_cities")
            {
                retVal.CountriesCities = getRelations(reader);
                Debug.WriteLine("countries_cities: {0}", retVal.CountriesCities.Count);
            }

            if (reader.TokenType == JsonToken.StartObject && reader.Path == "genres_subgenres")
            {
                retVal.GenresSubgenres = getRelations(reader);
                Debug.WriteLine("genres_subgenres: {0}", retVal.GenresSubgenres.Count);
            }

            if (reader.TokenType == JsonToken.StartArray && reader.Path == "countries")
            {
                retVal.Countries = getDictionary(reader);
                Debug.WriteLine("countries: {0}", retVal.Countries.Count);
            }

            if (reader.TokenType == JsonToken.StartArray && reader.Path == "cities")
            {
                retVal.Cities = getDictionary(reader);
                Debug.WriteLine("cities: {0}", retVal.Cities.Count);
            }

            if (reader.TokenType == JsonToken.StartArray && reader.Path == "genres")
            {
                retVal.Genres = getDictionary(reader);
                Debug.WriteLine("genres: {0}", retVal.Genres.Count);
            }

            if (reader.TokenType == JsonToken.StartArray && reader.Path == "subgenres")
            {
                retVal.Subgenres = getDictionary(reader);
                Debug.WriteLine("subgenres: {0}", retVal.Subgenres.Count);
            }

            if (reader.TokenType == JsonToken.StartArray && reader.Path == "stations")
            {
                retVal.Stations = getStations(reader);
                Debug.WriteLine("stations: {0}", retVal.Stations.Count);
            }
        }

        _logger.LogInformation("File parsed succesfully. Staion count: {0}", retVal.Stations.Count);
        Debug.WriteLine("ParseJsonService.Parse completed");

        return retVal;
    }
}
