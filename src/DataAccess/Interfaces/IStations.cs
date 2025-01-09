using PCRadio.DataAccess.Models;

namespace PCRadio.DataAccess.Interfaces;

public interface IStations
{
    IEnumerable<Station> GetFavorites();
    IEnumerable<Station> GetRecommended();
    IEnumerable<Station> GetByGenre(int genreId);
    IEnumerable<Station> GetBySubGenre(int subGenreId);
    IEnumerable<Station> GetByCountry(int countryId);
    IEnumerable<Station> GetByCity(int cityId);
    void SetFavorite(int stationId);
}
