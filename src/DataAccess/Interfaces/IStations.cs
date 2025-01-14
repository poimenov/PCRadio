using PCRadio.DataAccess.Models;

namespace PCRadio.DataAccess.Interfaces;

public class SearchParams
{
    public string? Name { get; set; }
    public int GenreId { get; set; }
    public int CountryId { get; set; }
}
public interface IStations
{
    IEnumerable<Station> GetFavorites(int skip, int take, out int totalCount);
    IEnumerable<Station> GetRecommended(int skip, int take, out int totalCount);
    IEnumerable<Station> GetByGenre(int id, int skip, int take, out int totalCount);
    IEnumerable<Station> Search(SearchParams? searchParams, int skip, int take, out int totalCount);
    void SetFavorite(int stationId, bool isFavorite);
    Station? GetStation(int id);
}
