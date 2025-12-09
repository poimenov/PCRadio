using Microsoft.EntityFrameworkCore;
using PCRadio.DataAccess.Interfaces;
using PCRadio.DataAccess.Models;

namespace PCRadio.DataAccess;

public class Stations : IStations
{
    public async Task<StationsResult> GetByGenreAsync(int id, int skip, int take)
    {
        await using (var db = new Database())
        {
            var query = db.Stations.Where(s => s.StationGenres!.Select(sg => sg.GenreId).Contains(id));
            var totalCount = await query.CountAsync();
            var stations = await query.Skip(skip).Take(take).ToListAsync();
            return new StationsResult
            {
                Stations = stations,
                TotalCount = totalCount
            };
        }
    }

    public async Task<StationsResult> GetFavoritesAsync(int skip, int take)
    {
        await using (var db = new Database())
        {
            var query = db.Stations.Where(s => s.IsFavorite);
            var totalCount = await query.CountAsync();
            var stations = await query.Skip(skip).Take(take).ToListAsync();
            return new StationsResult
            {
                Stations = stations,
                TotalCount = totalCount
            };
        }
    }

    public async Task<StationsResult> GetRecommendedAsync(int skip, int take)
    {
        await using (var db = new Database())
        {
            var query = db.Stations.Where(s => s.Recomended);
            var totalCount = await query.CountAsync();
            var stations = await query.Skip(skip).Take(take).ToListAsync();
            return new StationsResult
            {
                Stations = stations,
                TotalCount = totalCount
            };
        }
    }
    public async Task<Station?> GetStationAsync(int id)
    {
        await using (var db = new Database())
        {
            return await db.Stations
                .Include(s => s.Country)
                .Include(s => s.StationCities)!
                .ThenInclude(sc => sc.City)
                .Include(s => s.StationGenres)!
                .ThenInclude(sg => sg.Genre)
                .Include(s => s.StationSubGenres)!
                .ThenInclude(sg => sg.SubGenre)
                .FirstOrDefaultAsync(s => s.Id == id);
        }
    }

    public async Task<StationsResult> SearchAsync(SearchParams? searchParams, int skip, int take)
    {
        if (searchParams == null)
        {
            return new StationsResult
            {
                Stations = Enumerable.Empty<Station>(),
                TotalCount = 0
            };
        }
        await using (var db = new Database())
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

            var totalCount = await query.CountAsync();
            var stations = await query.Skip(skip).Take(take).ToListAsync();
            return new StationsResult
            {
                Stations = stations,
                TotalCount = totalCount
            };
        }
    }

    public async Task SetFavoriteAsync(int stationId, bool isFavorite)
    {
        await using (var db = new Database())
        {
            var station = await db.Stations.FirstOrDefaultAsync(s => s.Id == stationId);
            if (station != null)
            {
                station.IsFavorite = isFavorite;
                await db.SaveChangesAsync();
            }
        }
    }
}
