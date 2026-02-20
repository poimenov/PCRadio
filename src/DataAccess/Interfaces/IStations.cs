using PCRadio.DataAccess.Models;

namespace PCRadio.DataAccess.Interfaces;

public class SearchParams
{
    public string? Name { get; set; }
    public int GenreId { get; set; }
    public int CountryId { get; set; }
}
public class StationsResult
{
    public IEnumerable<Station> Stations { get; set; } = Enumerable.Empty<Station>();
    public int TotalCount { get; set; }
}
public class StationInfo
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string LogoUrl { get; set; } = string.Empty;
    public IEnumerable<string> Genres { get; set; } = Enumerable.Empty<string>();
}
public interface IStations
{
    Task<StationsResult> GetFavoritesAsync(int skip, int take);
    Task<StationsResult> GetRecommendedAsync(int skip, int take);
    Task<StationsResult> GetByGenreAsync(int id, int skip, int take);
    Task<StationsResult> SearchAsync(SearchParams? searchParams, int skip, int take);
    Task SetFavoriteAsync(int stationId, bool isFavorite);
    Task<Station?> GetStationAsync(int id);
    Task<IEnumerable<StationInfo>> ExportByGenreAsync(int id);
    Task<IEnumerable<StationInfo>> ExportFavoritesAsync();
    Task<IEnumerable<StationInfo>> ExportRecommendedAsync();
}
