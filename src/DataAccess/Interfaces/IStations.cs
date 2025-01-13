using PCRadio.DataAccess.Models;

namespace PCRadio.DataAccess.Interfaces;

public interface IStations
{
    IEnumerable<Station> GetFavorites(int skip, int take);
    IEnumerable<Station> GetRecommended(int skip, int take);
    IEnumerable<Station> GetByGenre(int id, int skip, int take);
    IEnumerable<Station> GetBySubGenre(int id, int skip, int take);
    IEnumerable<Station> GetByCountry(int id, int skip, int take);
    IEnumerable<Station> GetByCity(int id, int skip, int take);
    void SetFavorite(int stationId, bool isFavorite);
    int GetByGenreCount(int id);
    int GetBySubGenreCount(int id);
    int GetByCountryCount(int id);
    int GetByCityCount(int id);
    int GetFavoritesCount();
    int GetRecommendedCount();
}
