using Microsoft.EntityFrameworkCore;
using PCRadio.DataAccess.Interfaces;
using PCRadio.DataAccess.Models;

namespace PCRadio.DataAccess;

public class Stations : IStations
{
    public IEnumerable<Station> GetByCity(int cityId)
    {
        using (var db = new Database())
        {
            return db.Stations.Where(s => s.StationCities!.Select(sc => sc.CityId).Contains(cityId)).ToList();
        }
    }

    public IEnumerable<Station> GetByCountry(int countryId)
    {
        using (var db = new Database())
        {
            return db.Stations.Where(s => s.CountryId == countryId).ToList();
        }
    }

    public IEnumerable<Station> GetByGenre(int genreId)
    {
        using (var db = new Database())
        {
            return db.Stations.Where(s => s.StationGenres!.Select(sg => sg.GenreId).Contains(genreId)).Skip(0).Take(50).ToList();
        }
    }

    public IEnumerable<Station> GetBySubGenre(int subGenreId)
    {
        using (var db = new Database())
        {
            return db.Stations.Where(s => s.StationSubGenres!.Select(sg => sg.SubGenreId).Contains(subGenreId)).ToList();
        }
    }

    public IEnumerable<Station> GetFavorites()
    {
        using (var db = new Database())
        {
            return db.Stations.Where(s => s.IsFavorite).ToList();
        }
    }

    public IEnumerable<Station> GetRecommended()
    {
        using (var db = new Database())
        {
            return db.Stations.Where(s => s.Recomended).ToList();
        }
    }

    public void SetFavorite(int stationId)
    {
        using (var db = new Database())
        {
            var station = db.Stations.FirstOrDefault(s => s.Id == stationId);
            if (station != null)
            {
                station.IsFavorite = !station.IsFavorite;
                db.SaveChanges();
            }
        }
    }
}
