namespace PCRadio.Services.Interfaces;

public class ParseResult
{
    public Dictionary<int, int[]> CountriesCities { get; set; } = new Dictionary<int, int[]>();
    public Dictionary<int, int[]> GenresSubgenres { get; set; } = new Dictionary<int, int[]>();
    public Dictionary<int, string> Genres { get; set; } = new Dictionary<int, string>();
    public Dictionary<int, string> Subgenres { get; set; } = new Dictionary<int, string>();
    public Dictionary<int, string> Countries { get; set; } = new Dictionary<int, string>();
    public Dictionary<int, string> Cities { get; set; } = new Dictionary<int, string>();
    public Dictionary<int, ParseStation> Stations { get; set; } = new Dictionary<int, ParseStation>();
}

public class ParseStation
{
    public int Id { get; set; }
    public int Uid { get; set; }
    public string Stream { get; set; } = string.Empty;
    public bool Recomended { get; set; }
    public DateOnly Created { get; set; }
    public string Logo { get; set; } = string.Empty;
    public int Country_id { get; set; }
    public int[] Cities_ids { get; set; } = new int[0];
    public int[] Genres_ids { get; set; } = new int[0];
    public int[] Subgenres_ids { get; set; } = new int[0];
    public string Name { get; set; } = string.Empty;
    public string Descr { get; set; } = string.Empty;
    private string getIds(int[] ids)
    {
        return $"({string.Join(", ", ids)})";
    }
    public override string ToString()
    {
        return @$"
        Id: {Id}, 
        Uid: {Uid}, 
        Stream: {Stream}, 
        Recomended: {Recomended}, 
        Created: {Created}, 
        Logo: {Logo}, 
        Country_id: {Country_id}, 
        Cities_ids: {getIds(Cities_ids)}, 
        Genres_ids: {getIds(Genres_ids)}, 
        Subgenres_ids: {getIds(Subgenres_ids)}, 
        Name: {Name}, 
        Descr: {Descr}";
    }
}

public interface IParseJsonService
{
    ParseResult Parse(string jsonFilePath);
}
