using Microsoft.EntityFrameworkCore;
using PCRadio.DataAccess.Interfaces;
using PCRadio.DataAccess.Models;

namespace PCRadio.DataAccess;

public class Stations : IStations
{
    public IEnumerable<Station> GetByGenre(int id, int skip, int take, out int totalCount)
    {
        using (var db = new Database())
        {
            var query = db.Stations.Where(s => s.StationGenres!.Select(sg => sg.GenreId).Contains(id));
            totalCount = query.Count();
            return query.Skip(skip).Take(take).ToList();
        }
    }

    public IEnumerable<Station> GetFavorites(int skip, int take, out int totalCount)
    {
        using (var db = new Database())
        {
            var query = db.Stations.Where(s => s.IsFavorite);
            totalCount = query.Count();
            return query.Skip(skip).Take(take).ToList();
        }
    }

    public IEnumerable<Station> GetRecommended(int skip, int take, out int totalCount)
    {
        using (var db = new Database())
        {
            var query = db.Stations.Where(s => s.Recomended);
            totalCount = query.Count();
            return query.Skip(skip).Take(take).ToList();
        }
    }

    public Station? GetStation(int id)
    {
        using (var db = new Database())
        {
            return db.Stations
                .Include(s => s.Country)
                .Include(s => s.StationCities)!
                .ThenInclude(sc => sc.City)
                .Include(s => s.StationGenres)!
                .ThenInclude(sg => sg.Genre)
                .Include(s => s.StationSubGenres)!
                .ThenInclude(sg => sg.SubGenre)
                .FirstOrDefault(s => s.Id == id);
        }
    }

    public IEnumerable<Station> Search(SearchParams? searchParams, int skip, int take, out int totalCount)
    {
        if (searchParams == null)
        {
            totalCount = 0;
            return new List<Station>();
        }

        using (var db = new Database())
        {
            var query = db.Stations.AsQueryable();
            if (!string.IsNullOrWhiteSpace(searchParams.Name))
            {
                query = query.Where(s => EF.Functions.Like(s.Name, $"%{searchParams.Name.Trim()}%"));
            }

            if (searchParams.GenreId > 0)
            {
                query = query.Where(s => s.StationGenres!.Select(sg => sg.GenreId).Contains(searchParams.GenreId));
            }

            if (searchParams.CountryId != 0)
            {
                query = query.Where(s => s.CountryId == searchParams.CountryId);
            }

            totalCount = query.Count();
            return query.Skip(skip).Take(take).ToList();
        }
    }

    public void SetFavorite(int stationId, bool isFavorite)
    {
        using (var db = new Database())
        {
            var station = db.Stations.FirstOrDefault(s => s.Id == stationId);
            if (station != null)
            {
                station.IsFavorite = isFavorite;
                db.SaveChanges();
            }
        }
    }
}
