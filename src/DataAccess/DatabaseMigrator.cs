using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PCRadio.DataAccess.Interfaces;

namespace PCRadio.DataAccess;

public class DatabaseMigrator : IDatabaseMigrator
{
    private readonly ILogger<DatabaseMigrator> _logger;

    public DatabaseMigrator(ILogger<DatabaseMigrator> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Asynchronously migrates the database.
    /// </summary>
    public async Task MigrateDatabaseAsync(string databasePath)
    {
        try
        {
            _logger.LogInformation("Starting async database migration");

            if (string.IsNullOrWhiteSpace(databasePath))
            {
                throw new ArgumentException("Database path cannot be null or whitespace.", nameof(databasePath));
            }

            // Ensure app data directory exists
            try
            {
                var databaseDirectory = Path.GetDirectoryName(Path.GetFullPath(databasePath));
                if (string.IsNullOrEmpty(databaseDirectory))
                {
                    throw new InvalidOperationException("Database directory could not be determined.");
                }

                if (!Directory.Exists(databaseDirectory))
                {
                    Directory.CreateDirectory(databaseDirectory);
                    _logger.LogInformation("Created database directory: {DatabaseDirectory}", databaseDirectory);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create database directory for: {DatabasePath}", databasePath);
                throw;
            }

            var dbPath = Path.GetFullPath(databasePath);
            var dbExists = File.Exists(dbPath);

            // Run migrations asynchronously
            await using (var database = new Database(dbPath, pooling: false))
            {
                try
                {
                    await database.Database.MigrateAsync();

                    if (dbExists)
                    {
                        _logger.LogInformation("Database migrated successfully: {DbPath}", dbPath);
                    }
                    else
                    {
                        _logger.LogInformation("Database created successfully: {DbPath}", dbPath);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Database migration failed: {DbPath}", dbPath);
                    throw;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Critical error during async database migration");
            throw;
        }
    }
}