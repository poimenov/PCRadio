using Microsoft.EntityFrameworkCore;
using PCRadio.DataAccess.Interfaces;
using PCRadio.DataAccess.Models;

namespace PCRadio.DataAccess;

public class Stations : IStations
{
    public IEnumerable<Station> GetByCity(int id, int skip, int take)
    {
        using (var db = new Database())
        {
            return db.Stations.Where(s => s.StationCities!.Select(sc => sc.CityId).Contains(id)).Skip(skip).Take(take).ToList();
        }
    }

    public int GetByCityCount(int id)
    {
        using (var db = new Database())
        {
            return db.Stations.Where(s => s.StationCities!.Select(sc => sc.CityId).Contains(id)).Count();
        }
    }

    public IEnumerable<Station> GetByCountry(int id, int skip, int take)
    {
        using (var db = new Database())
        {
            return db.Stations.Where(s => s.CountryId == id).Skip(skip).Take(take).ToList();
        }
    }

    public int GetByCountryCount(int id)
    {
        using (var db = new Database())
        {
            return db.Stations.Where(s => s.CountryId == id).Count();
        }
    }

    public IEnumerable<Station> GetByGenre(int id, int skip, int take)
    {
        using (var db = new Database())
        {
            return db.Stations.Where(s => s.StationGenres!.Select(sg => sg.GenreId).Contains(id)).Skip(skip).Take(take).ToList();
        }
    }

    public int GetByGenreCount(int id)
    {
        using (var db = new Database())
        {
            return db.Stations.Where(s => s.StationGenres!.Select(sg => sg.GenreId).Contains(id)).Count();
        }
    }

    public IEnumerable<Station> GetBySubGenre(int id, int skip, int take)
    {
        using (var db = new Database())
        {
            return db.Stations.Where(s => s.StationSubGenres!.Select(ss => ss.SubGenreId).Contains(id)).Skip(skip).Take(take).ToList();
        }
    }

    public int GetBySubGenreCount(int id)
    {
        using (var db = new Database())
        {
            return db.Stations.Where(s => s.StationSubGenres!.Select(ss => ss.SubGenreId).Contains(id)).Count();
        }
    }

    public IEnumerable<Station> GetFavorites(int skip, int take)
    {
        using (var db = new Database())
        {
            return db.Stations.Where(s => s.IsFavorite).Skip(skip).Take(take).ToList();
        }
    }

    public int GetFavoritesCount()
    {
        using (var db = new Database())
        {
            return db.Stations.Where(s => s.IsFavorite).Count();
        }
    }

    public IEnumerable<Station> GetRecommended(int skip, int take)
    {
        using (var db = new Database())
        {
            return db.Stations.Where(s => s.Recomended).Skip(skip).Take(take).ToList();
        }
    }

    public int GetRecommendedCount()
    {
        using (var db = new Database())
        {
            return db.Stations.Where(s => s.Recomended).Count();
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
