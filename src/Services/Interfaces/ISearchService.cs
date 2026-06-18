namespace PCRadio.Services.Interfaces;

public class Track
{
    public Track(int id, string title, string artist, int artist_id, string href, int storage_group, string date_create, string icon, int duration)
    {
        Id = id;
        Title = title;
        Artist = artist;
        Artist_Id = artist_id;
        Href = href;
        Storage_Group = storage_group;
        Date_Create = date_create;
        Icon = icon;
        Duration = duration;
        _duration = TimeSpan.FromSeconds(duration);


    }
    public int Id { get; set; }
    public string Title { get; set; }
    public string Artist { get; set; }
    public int Artist_Id { get; set; }
    public string Href { get; set; }
    public int Storage_Group { get; set; }
    public string Date_Create { get; set; }
    public string Icon { get; set; }
    public int Duration { get; set; }
    private TimeSpan _duration { get; set; }
    public string DisplayDuration => String.Format("{0:D2}:{1:D2}", _duration.Minutes, _duration.Seconds);
    public string FileName => Href.Split('/').Last();
}
public interface ISearchService
{
    Task<IEnumerable<Track>> Search(string keyword);
    Task Download(Track track);
}