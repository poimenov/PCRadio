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

    public DataLoader(
        ILogger<DataLoader> logger,
        IOptions<AppSettings> options,
        IDatabaseMigrator databaseMigrator)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _appSettings = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _databaseMigrator = databaseMigrator ?? throw new ArgumentNullException(nameof(databaseMigrator));
    }

    public async Task<bool> LoadDataAsync(ParseResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        ValidateParseResult(result);

        var startedAt = DateTime.UtcNow;
        var databasePath = Database.GetDatabasePath(_appSettings.AppDataPath);
        var databaseDirectory = Path.GetDirectoryName(databasePath)
            ?? throw new InvalidOperationException("Database directory could not be determined.");
        Directory.CreateDirectory(databaseDirectory);

        var stagingPath = $"{databasePath}.{Guid.NewGuid():N}.staging";
        var backupPath = $"{databasePath}.bak";
        var backupStagingPath = $"{databasePath}.{Guid.NewGuid():N}.backup-staging";
        var preserveBackupStaging = false;

        try
        {
            var favorites = new HashSet<int>();
            var historyRecords = new List<HistoryRecord>();
            if (File.Exists(databasePath))
            {
                var hasMigrationHistory = await HasMigrationHistoryTableAsync(databasePath);
                if (hasMigrationHistory)
                {
                    await using var existingDatabase = new Database(databasePath, pooling: false);
                    favorites = (await existingDatabase.Stations
                        .Where(station => station.IsFavorite)
                        .Select(station => station.Id)
                        .ToListAsync()).ToHashSet();

                    var migrationIds = await existingDatabase.Database
                        .SqlQueryRaw<string>("SELECT MigrationId FROM __EFMigrationsHistory")
                        .ToListAsync();
                    if (migrationIds.Any(id => id.Contains("AddedHistory", StringComparison.Ordinal)))
                    {
                        historyRecords = await existingDatabase.HistoryRecords.ToListAsync();
                    }
                }
            }

            _logger.LogInformation("Creating staged database at {StagingPath}", stagingPath);
            await _databaseMigrator.MigrateDatabaseAsync(stagingPath);
            await PopulateStagingDatabaseAsync(stagingPath, result, favorites, historyRecords);
            await ValidateStagingDatabaseAsync(stagingPath, result, historyRecords.Count);

            if (File.Exists(databasePath))
            {
                File.Replace(stagingPath, databasePath, backupStagingPath);
                try
                {
                    File.Move(backupStagingPath, backupPath, overwrite: true);
                }
                catch (Exception ex)
                {
                    preserveBackupStaging = true;
                    _logger.LogError(
                        ex,
                        "Database was updated, but the rolling backup could not be rotated. The previous database is preserved at {BackupStagingPath}",
                        backupStagingPath);
                }

                if (!preserveBackupStaging)
                {
                    _logger.LogInformation(
                        "Database replaced atomically; previous database saved as rolling backup at {BackupPath}",
                        backupPath);
                    try
                    {
                        DeleteLegacyBackups(databasePath, databaseDirectory);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to remove older database backups from {Directory}", databaseDirectory);
                    }
                }
            }
            else
            {
                File.Move(stagingPath, databasePath);
                _logger.LogInformation("Staged database moved into place at {DatabasePath}", databasePath);
            }

            _logger.LogInformation("Database update completed in {ElapsedMilliseconds}ms",
                (DateTime.UtcNow - startedAt).TotalMilliseconds);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database update failed; the existing database was left unchanged");
            return false;
        }
        finally
        {
            DeleteIfExists(stagingPath);
            DeleteIfExists(stagingPath + "-wal");
            DeleteIfExists(stagingPath + "-shm");
            if (!preserveBackupStaging)
            {
                DeleteIfExists(backupStagingPath);
            }
        }
    }

    private async Task PopulateStagingDatabaseAsync(
        string stagingPath,
        ParseResult result,
        HashSet<int> favorites,
        List<HistoryRecord> historyRecords)
    {
        await using var database = new Database(stagingPath, pooling: false);
        await using var transaction = await database.Database.BeginTransactionAsync();

        await database.Countries.AddRangeAsync(
            result.Countries.Select(country => new Country { Id = country.Key, Name = country.Value }));
        await database.SaveChangesAsync();

        foreach (var country in result.CountriesCities)
        {
            var countryEntity = await database.Countries.FirstAsync(item => item.Id == country.Key);
            var cities = country.Value.Select(cityId => new City
            {
                Id = cityId,
                Name = result.Cities[cityId],
                CountryId = country.Key,
                Country = countryEntity
            });
            await database.Cities.AddRangeAsync(cities);
        }

        await database.SaveChangesAsync();

        await database.Genres.AddRangeAsync(
            result.Genres.Select(genre => new Genre { Id = genre.Key, Name = genre.Value }));
        await database.SaveChangesAsync();

        foreach (var genre in result.GenresSubgenres)
        {
            var genreEntity = await database.Genres.FirstAsync(item => item.Id == genre.Key);
            var subgenres = genre.Value.Select(subgenreId => new SubGenre
            {
                Id = subgenreId,
                Name = result.Subgenres[subgenreId],
                GenreId = genre.Key,
                Genre = genreEntity
            });
            await database.SubGenres.AddRangeAsync(subgenres);
        }

        await database.SaveChangesAsync();

        await database.Stations.AddRangeAsync(result.Stations.Select(item => new Station
        {
            Id = item.Key,
            UId = item.Value.Uid,
            Stream = item.Value.Stream,
            Recomended = item.Value.Recomended,
            Created = item.Value.Created,
            Logo = item.Value.Logo,
            CountryId = item.Value.Country_id,
            Name = item.Value.Name,
            Description = item.Value.Descr,
            IsFavorite = favorites.Contains(item.Key)
        }));
        await database.SaveChangesAsync();

        var stationEntities = database.Stations.Local.ToDictionary(station => station.Id);
        var cityEntities = database.Cities.Local.ToDictionary(city => city.Id);
        var genreEntities = database.Genres.Local.ToDictionary(genre => genre.Id);
        var subgenreEntities = database.SubGenres.Local.ToDictionary(subgenre => subgenre.Id);

        await database.StationCities.AddRangeAsync(result.Stations.Values.SelectMany(station =>
            station.Cities_ids.Select(cityId => new StationCity
            {
                StationId = station.Id,
                Station = stationEntities[station.Id],
                CityId = cityId,
                City = cityEntities[cityId]
            })));
        await database.StationGenres.AddRangeAsync(result.Stations.Values.SelectMany(station =>
            station.Genres_ids.Select(genreId => new StationGenre
            {
                StationId = station.Id,
                Station = stationEntities[station.Id],
                GenreId = genreId,
                Genre = genreEntities[genreId]
            })));
        await database.StationSubGenres.AddRangeAsync(result.Stations.Values.SelectMany(station =>
            station.Subgenres_ids.Select(subgenreId => new StationSubGenre
            {
                StationId = station.Id,
                Station = stationEntities[station.Id],
                SubGenreId = subgenreId,
                SubGenre = subgenreEntities[subgenreId]
            })));

        if (historyRecords.Count > 0)
        {
            await database.HistoryRecords.AddRangeAsync(historyRecords);
        }

        await database.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    private static async Task ValidateStagingDatabaseAsync(
        string stagingPath,
        ParseResult result,
        int expectedHistoryCount)
    {
        await using var database = new Database(stagingPath, pooling: false);

        if (await database.Countries.CountAsync() != result.Countries.Count
            || await database.Cities.CountAsync() != result.CountriesCities.Values.Sum(ids => ids.Length)
            || await database.Genres.CountAsync() != result.Genres.Count
            || await database.SubGenres.CountAsync() != result.GenresSubgenres.Values.Sum(ids => ids.Length)
            || await database.Stations.CountAsync() != result.Stations.Count
            || await database.StationCities.CountAsync() != result.Stations.Values.Sum(station => station.Cities_ids.Length)
            || await database.StationGenres.CountAsync() != result.Stations.Values.Sum(station => station.Genres_ids.Length)
            || await database.StationSubGenres.CountAsync() != result.Stations.Values.Sum(station => station.Subgenres_ids.Length)
            || await database.HistoryRecords.CountAsync() != expectedHistoryCount)
        {
            throw new InvalidDataException("Staged database contents do not match the validated import data.");
        }

        var connection = (SqliteConnection)database.Database.GetDbConnection();
        await connection.OpenAsync();

        await using (var integrityCommand = connection.CreateCommand())
        {
            integrityCommand.CommandText = "PRAGMA integrity_check;";
            var integrityResult = Convert.ToString(await integrityCommand.ExecuteScalarAsync());
            if (!string.Equals(integrityResult, "ok", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException($"SQLite integrity check failed: {integrityResult}");
            }
        }

        await using (var foreignKeyCommand = connection.CreateCommand())
        {
            foreignKeyCommand.CommandText = "PRAGMA foreign_key_check;";
            await using var reader = await foreignKeyCommand.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                throw new InvalidDataException(
                    $"SQLite foreign key check failed for table '{reader.GetString(0)}'.");
            }
        }
    }

    private static void ValidateParseResult(ParseResult result)
    {
        if (result.Countries.Count == 0 || result.Genres.Count == 0 || result.Stations.Count == 0)
        {
            throw new InvalidDataException("Import data must contain countries, genres, and stations.");
        }

        ValidateRelations(result.CountriesCities, result.Countries.Keys, result.Cities.Keys, "country/city");
        ValidateRelations(result.GenresSubgenres, result.Genres.Keys, result.Subgenres.Keys, "genre/subgenre");

        var mappedCityIds = result.CountriesCities.Values.SelectMany(ids => ids).ToArray();
        var mappedSubgenreIds = result.GenresSubgenres.Values.SelectMany(ids => ids).ToArray();
        if (mappedCityIds.Length != result.Cities.Count
            || mappedCityIds.Distinct().Count() != result.Cities.Count
            || mappedSubgenreIds.Length != result.Subgenres.Count
            || mappedSubgenreIds.Distinct().Count() != result.Subgenres.Count)
        {
            throw new InvalidDataException("Import data contains unmapped or multiply mapped cities or subgenres.");
        }

        foreach (var station in result.Stations)
        {
            var value = station.Value;
            if (station.Key <= 0
                || value.Id != station.Key
                || string.IsNullOrWhiteSpace(value.Name)
                || string.IsNullOrWhiteSpace(value.Stream)
                || !result.Countries.ContainsKey(value.Country_id)
                || value.Cities_ids is null
                || value.Genres_ids is null
                || value.Subgenres_ids is null
                || value.Cities_ids.Distinct().Count() != value.Cities_ids.Length
                || value.Genres_ids.Distinct().Count() != value.Genres_ids.Length
                || value.Subgenres_ids.Distinct().Count() != value.Subgenres_ids.Length
                || value.Cities_ids.Any(id => !result.Cities.ContainsKey(id))
                || value.Genres_ids.Any(id => !result.Genres.ContainsKey(id))
                || value.Subgenres_ids.Any(id => !result.Subgenres.ContainsKey(id)))
            {
                throw new InvalidDataException($"Import data contains an invalid station record with ID {station.Key}.");
            }
        }
    }

    private static void ValidateRelations(
        Dictionary<int, int[]> relations,
        IEnumerable<int> sourceIds,
        IEnumerable<int> targetIds,
        string relationName)
    {
        var validSources = sourceIds.ToHashSet();
        var validTargets = targetIds.ToHashSet();
        foreach (var relation in relations)
        {
            if (!validSources.Contains(relation.Key)
                || relation.Value is null
                || relation.Value.Any(id => !validTargets.Contains(id))
                || relation.Value.Distinct().Count() != relation.Value.Length)
            {
                throw new InvalidDataException($"Import data contains an invalid {relationName} relation.");
            }
        }
    }

    private static async Task<bool> HasMigrationHistoryTableAsync(string filePath)
    {
        await using var connection = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = filePath,
            Mode = SqliteOpenMode.ReadOnly
        }.ToString());
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT 1 FROM sqlite_master WHERE type = 'table' AND name = '__EFMigrationsHistory' LIMIT 1;";
        return await command.ExecuteScalarAsync() is not null;
    }

    private void DeleteIfExists(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to clean up staging file {Path}", path);
        }
    }

    private void DeleteLegacyBackups(string databasePath, string databaseDirectory)
    {
        var databaseFileName = Path.GetFileName(databasePath);
        var prefix = $"{databaseFileName}.";
        const string suffix = ".bak";

        foreach (var path in Directory.EnumerateFiles(
                     databaseDirectory,
                     $"{databaseFileName}.*{suffix}",
                     SearchOption.TopDirectoryOnly))
        {
            var fileName = Path.GetFileName(path);
            if (fileName == $"{databaseFileName}{suffix}"
                || !fileName.StartsWith(prefix, StringComparison.Ordinal)
                || !fileName.EndsWith(suffix, StringComparison.Ordinal))
            {
                continue;
            }

            var legacyName = fileName[prefix.Length..^suffix.Length];
            var segments = legacyName.Split('.');
            if (segments.Length == 2
                && segments[0].Length == 17
                && segments[0].All(char.IsAsciiDigit)
                && Guid.TryParseExact(segments[1], "N", out _))
            {
                DeleteIfExists(path);
            }
        }
    }
}
