using System.Diagnostics;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PCRadio.DataAccess.Interfaces;
using PCRadio.DataAccess.Models;
using PCRadio.Services.Interfaces;

namespace PCRadio.DataAccess;

public class DataLoader : IDataLoader
{
    private readonly ILogger<DataLoader> _logger;
    private readonly AppSettings _appSettings;
    private readonly IDatabaseMigrator _databaseMigrator;
    public DataLoader(ILogger<DataLoader> logger, IOptions<AppSettings> options, IDatabaseMigrator databaseMigrator)
    {
        _logger = logger;
        _appSettings = options.Value;
        _databaseMigrator = databaseMigrator;
    }
    public async Task LoadDataAsync(ParseResult result)
    {
        _logger.LogInformation("Loading data started");
        var start = DateTime.Now;
        var favorites = Enumerable.Empty<int>();
        var success = false;
        var sql = string.Empty;
        var filePath = Path.Combine(_appSettings.AppDataPath, Database.DB_FILE_NAME);
        var bakFilePath = Path.Combine(_appSettings.AppDataPath, Database.DB_FILE_NAME + ".bak");
        if (File.Exists(filePath))
        {
            // backup database
            File.Copy(filePath, bakFilePath, true);

            // select favorites and delete database
            using (var db = new Database())
            {
                favorites = db.Stations.Where(s => s.IsFavorite).Select(s => s.Id).ToList();
                db.Database.EnsureDeleted();
            }

            Debug.WriteLine($"Database was backuped ({DateTime.Now - start})");
        }

        _databaseMigrator.MigrateDatabase();
        Debug.WriteLine($"Database migrated ({DateTime.Now - start})");

        using (var db = new Database())
        {
            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    await db.Countries.AddRangeAsync(result.Countries.Select(c => new Country { Id = c.Key, Name = c.Value }));
                    await db.SaveChangesAsync();
                    Debug.WriteLine($"Added countries ({DateTime.Now - start})");

                    foreach (var country in result.CountriesCities)
                    {
                        var currCountry = await db.Countries.FirstAsync(c => c.Id == country.Key);
                        var cities = result.Cities.Where(c => country.Value.Contains(c.Key))
                            .Select(c => new City { Id = c.Key, Name = c.Value, CountryId = country.Key, Country = currCountry });
                        await db.Cities.AddRangeAsync(cities);
                    }

                    await db.SaveChangesAsync();
                    Debug.WriteLine($"Added cities ({DateTime.Now - start})");

                    await db.Genres.AddRangeAsync(result.Genres.Select(g => new Genre { Id = g.Key, Name = g.Value }));
                    await db.SaveChangesAsync();
                    Debug.WriteLine($"Added genres ({DateTime.Now - start})");

                    foreach (var genre in result.GenresSubgenres)
                    {
                        var currGenre = await db.Genres.FirstAsync(g => g.Id == genre.Key);
                        var Subgenres = result.Subgenres.Where(s => genre.Value.Contains(s.Key))
                            .Select(s => new SubGenre { Id = s.Key, Name = s.Value, GenreId = genre.Key, Genre = currGenre });
                        await db.SubGenres.AddRangeAsync(Subgenres);
                    }

                    await db.SaveChangesAsync();
                    Debug.WriteLine($"Added subgenres ({DateTime.Now - start})");

                    await db.Stations.AddRangeAsync(result.Stations.Select(s => new Station
                    {
                        Id = s.Key,
                        UId = s.Value.Uid,
                        Stream = s.Value.Stream,
                        Recomended = s.Value.Recomended,
                        Created = s.Value.Created,
                        Logo = s.Value.Logo,
                        CountryId = s.Value.Country_id,
                        Name = s.Value.Name,
                        Description = s.Value.Descr,
                        IsFavorite = favorites.Contains(s.Key)
                    }));

                    await db.SaveChangesAsync();
                    Debug.WriteLine($"Added stations ({DateTime.Now - start})");

                    foreach (var station in result.Stations.Where(s => s.Value.Cities_ids.Any()))
                    {
                        sql = string.Join(';', station.Value.Cities_ids.Select(c =>
                            $"INSERT INTO StationCities (StationId, CityId) VALUES ({station.Value.Id}, {c});").ToArray());
                        await db.Database.ExecuteSqlRawAsync(sql);
                    }

                    await db.SaveChangesAsync();
                    Debug.WriteLine($"Added station cities ({DateTime.Now - start})");

                    foreach (var station in result.Stations.Where(s => s.Value.Genres_ids.Any()))
                    {
                        sql = string.Join(';', station.Value.Genres_ids.Select(g =>
                            $"INSERT INTO StationGenres (StationId, GenreId) VALUES ({station.Value.Id}, {g});").ToArray());
                        await db.Database.ExecuteSqlRawAsync(sql);
                    }

                    await db.SaveChangesAsync();
                    Debug.WriteLine($"Added station genres ({DateTime.Now - start})");

                    foreach (var station in result.Stations.Where(s => s.Value.Subgenres_ids.Any()))
                    {
                        sql = string.Join(';', station.Value.Subgenres_ids.Select(sg =>
                            $"INSERT INTO StationSubGenres (StationId, SubGenreId) VALUES ({station.Value.Id}, {sg});").ToArray());
                        await db.Database.ExecuteSqlRawAsync(sql);
                    }

                    await db.SaveChangesAsync();
                    Debug.WriteLine($"Added station subgenres ({DateTime.Now - start})");

                    await transaction.CommitAsync();
                    success = true;
                    _logger.LogInformation("Loading data completed");
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    Debug.WriteLine(ex.Message);
                    _logger.LogError(ex, "Loading data failed");
                    if (ex.InnerException != null)
                    {
                        Debug.WriteLine(ex.InnerException);
                        _logger.LogError(ex.InnerException, ex.InnerException.Message);
                    }
                }
            }
        }

        if (!success && File.Exists(bakFilePath))
        {
            File.Copy(bakFilePath, filePath, true);
        }
    }
}
