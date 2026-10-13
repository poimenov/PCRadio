using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PCRadio.Common;
using PCRadio.DataAccess.Interfaces;
using PCRadio.DataAccess.Models;

namespace PCRadio.DataAccess;

public class Stations : IStations
{
    private readonly ILogger<Stations> _logger;
    private readonly string _databasePath;

    public Stations(ILogger<Stations> logger, IOptions<AppSettings> options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _databasePath = Database.GetDatabasePath(options.Value.AppDataPath);
    }

    private static async Task PopulateStationGenresAsync(Database db, List<StationInfo> stations)
    {
        if (stations.Count == 0)
        {
            return;
        }

        var stationIds = stations.Select(station => station.Id).ToArray();
        var genres = await db.StationGenres
            .Where(stationGenre => stationIds.Contains(stationGenre.StationId))
            .Select(stationGenre => new
            {
                StationId = stationGenre.StationId,
                Name = stationGenre.Genre!.Name
            })
            .Concat(db.StationSubGenres
                .Where(stationSubGenre => stationIds.Contains(stationSubGenre.StationId))
                .Select(stationSubGenre => new
                {
                    StationId = stationSubGenre.StationId,
                    Name = stationSubGenre.SubGenre!.Name
                }))
            .Distinct()
            .ToListAsync();

        var genresByStation = genres
            .GroupBy(genre => genre.StationId)
            .ToDictionary(group => group.Key, group => group.Select(genre => genre.Name).ToArray());

        foreach (var station in stations)
        {
            station.Genres = genresByStation.TryGetValue(station.Id, out var stationGenres)
                ? stationGenres
                : Enumerable.Empty<string>();
        }
    }

    public async Task<IEnumerable<StationInfo>> ExportByGenreAsync(int id)
    {
        if (id <= 0)
        {
            return Enumerable.Empty<StationInfo>();
        }

        await using (var db = new Database(_databasePath))
        {
            if (!await db.Genres.AnyAsync(g => g.Id == id))
            {
                return Enumerable.Empty<StationInfo>();
            }

            var retVal = await db.Stations.Where(s => s.StationGenres!.Select(sg => sg.GenreId).Contains(id)).Select(s => new StationInfo
            {
                Id = s.Id,
                Name = s.Name,
                Url = s.Stream,
                LogoUrl = s.Logo,
                Genres = Enumerable.Empty<string>()
            }).ToListAsync();

            await PopulateStationGenresAsync(db, retVal);

            return retVal;
        }
    }

    public async Task<IEnumerable<StationInfo>> ExportFavoritesAsync()
    {
        await using (var db = new Database(_databasePath))
        {
            var retVal = await db.Stations.Where(s => s.IsFavorite).Select(s => new StationInfo
            {
                Id = s.Id,
                Name = s.Name,
                Url = s.Stream,
                LogoUrl = s.Logo,
                Genres = Enumerable.Empty<string>()
            }).ToListAsync();

            await PopulateStationGenresAsync(db, retVal);
            return retVal;
        }
    }

    public async Task<IEnumerable<StationInfo>> ExportRecommendedAsync()
    {
        try
        {
            await using (var db = new Database(_databasePath))
            {
                var retVal = await db.Stations.Where(s => s.Recomended).Select(s => new StationInfo
                {
                    Id = s.Id,
                    Name = s.Name,
                    Url = s.Stream,
                    LogoUrl = s.Logo,
                    Genres = Enumerable.Empty<string>()
                }).ToListAsync();

                await PopulateStationGenresAsync(db, retVal);
                return retVal;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while exporting recommended stations.");
            return Enumerable.Empty<StationInfo>();
        }
    }

    public async Task<StationsResult> GetByGenreAsync(int id, int skip, int take)
    {
        await using (var db = new Database(_databasePath))
        {
            var query = db.Stations.Where(s => s.StationGenres!.Select(sg => sg.GenreId).Contains(id));
            var totalCount = await query.CountAsync();
            var stations = await query.OrderBy(station => station.Id).Skip(skip).Take(take).ToListAsync();
            return new StationsResult
            {
                Stations = stations,
                TotalCount = totalCount
            };
        }
    }

    public async Task<StationsResult> GetFavoritesAsync(int skip, int take)
    {
        await using (var db = new Database(_databasePath))
        {
            var query = db.Stations.Where(s => s.IsFavorite);
            var totalCount = await query.CountAsync();
            var stations = await query.OrderBy(station => station.Id).Skip(skip).Take(take).ToListAsync();
            return new StationsResult
            {
                Stations = stations,
                TotalCount = totalCount
            };
        }
    }

    public async Task<StationsResult> GetRecommendedAsync(int skip, int take)
    {
        try
        {
            await using (var db = new Database(_databasePath))
            {
                var query = db.Stations.Where(s => s.Recomended);
                var totalCount = await query.CountAsync();
                var stations = await query.OrderBy(station => station.Id).Skip(skip).Take(take).ToListAsync();
                return new StationsResult
                {
                    Stations = stations,
                    TotalCount = totalCount
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while fetching recommended stations.");
            return new StationsResult
            {
                Stations = Enumerable.Empty<Station>(),
                TotalCount = 0
            };
        }
    }
    public async Task<Station?> GetStationAsync(int id)
    {
        await using (var db = new Database(_databasePath))
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
        await using (var db = new Database(_databasePath))
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
            var stations = await query.OrderBy(station => station.Id).Skip(skip).Take(take).ToListAsync();
            return new StationsResult
            {
                Stations = stations,
                TotalCount = totalCount
            };
        }
    }

    public async Task SetFavoriteAsync(int stationId, bool isFavorite)
    {
        await using (var db = new Database(_databasePath))
        {
            var station = await db.Stations.FirstOrDefaultAsync(s => s.Id == stationId);
            if (station != null)
            {
                station.IsFavorite = isFavorite;
                await db.SaveChangesAsync();
            }
        }
    }

    public async Task<OperationResult<Station>> AddStationAsync(Station station)
    {
        var retVal = new OperationResult<Station>
        {
            Success = false,
            Result = new Station()
            {
                Name = string.Empty,
                Logo = string.Empty,
                Stream = string.Empty
            }
        };

        await using (var db = new Database(_databasePath))
        {
            if (!await db.Stations.AnyAsync(s => s.Stream.ToLower() == station.Stream.ToLower()))
            {
                await using (var transaction = await db.Database.BeginTransactionAsync())
                {
                    try
                    {
                        var stationToAdd = new Station()
                        {
                            CountryId = station.CountryId,
                            Name = station.Name,
                            Logo = station.Logo ?? string.Empty,
                            Stream = station.Stream,
                            Description = station.Description,
                            IsFavorite = station.IsFavorite,
                            Created = DateOnly.FromDateTime(DateTime.Now),
                            Recomended = false,
                            UId = 0
                        };
                        await db.Stations.AddAsync(stationToAdd);
                        await db.SaveChangesAsync();
                        var stationId = stationToAdd.Id;
                        if (station.StationGenres != null && station.StationGenres.Any())
                        {
                            foreach (var sg in station.StationGenres)
                            {
                                var genre = await db.Genres.FirstOrDefaultAsync(g => g.Id == sg.GenreId);
                                if (genre is not null)
                                {
                                    var stationGenreToAdd = new StationGenre()
                                    {
                                        Genre = genre,
                                        GenreId = genre.Id,
                                        Station = stationToAdd,
                                        StationId = stationId
                                    };
                                    await db.StationGenres.AddAsync(stationGenreToAdd);
                                }
                            }
                        }

                        if (station.StationSubGenres != null && station.StationSubGenres.Any())
                        {
                            foreach (var sg in station.StationSubGenres)
                            {
                                var subgenre = await db.SubGenres.FirstOrDefaultAsync(g => g.Id == sg.Id);
                                if (subgenre is not null)
                                {
                                    var stationSubgenreToAdd = new StationSubGenre()
                                    {
                                        SubGenre = subgenre,
                                        SubGenreId = subgenre.Id,
                                        Station = stationToAdd,
                                        StationId = stationId
                                    };
                                    await db.StationSubGenres.AddAsync(sg);
                                }

                            }
                        }

                        await db.SaveChangesAsync();
                        await transaction.CommitAsync();

                        var newStation = await db.Stations
                            .Include(s => s.Country)
                            .Include(s => s.StationGenres)!
                            .ThenInclude(sg => sg.Genre)
                            .FirstOrDefaultAsync(s => s.Id == stationId);
                        retVal.Result = newStation;
                        retVal.Success = true;
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        retVal.Message = ex.Message;
                        retVal.Exception = ex;
                    }
                }
            }
            else
            {
                retVal.Message = "A station with the same stream already exists";
            }
        }

        return retVal;
    }

    public async Task<OperationResult> UpdateStationAsync(Station station)
    {
        var retVal = new OperationResult { Success = false };
        await using (var db = new Database(_databasePath))
        {
            if (await db.Stations.AnyAsync(s => s.Id == station.Id)
                && !await db.Stations.AnyAsync(s => s.Id != station.Id && s.Stream == station.Stream))
            {
                await using (var transaction = await db.Database.BeginTransactionAsync())
                {
                    try
                    {
                        var existedStation = await db.Stations.FirstAsync(s => s.Id == station.Id);
                        existedStation.CountryId = station.CountryId;
                        existedStation.Name = station.Name;
                        existedStation.Logo = station.Logo;
                        existedStation.Description = station.Description;
                        existedStation.IsFavorite = station.IsFavorite;
                        existedStation.Stream = station.Stream;

                        await db.SaveChangesAsync();
                        await transaction.CommitAsync();
                        retVal.Success = true;
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        retVal.Message = ex.Message;
                        retVal.Exception = ex;
                    }
                }
            }
            else
            {
                retVal.Message = "Station not found or station with the same Stream exist";
            }
        }

        return retVal;
    }
}
