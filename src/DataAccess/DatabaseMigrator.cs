using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PCRadio.DataAccess.Interfaces;

namespace PCRadio.DataAccess;

public class DatabaseMigrator : IDatabaseMigrator
{
    private readonly ILogger<DatabaseMigrator> _logger;
    private readonly AppSettings _settings;

    public DatabaseMigrator(ILogger<DatabaseMigrator> logger, IOptions<AppSettings> options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _settings = options?.Value ?? throw new ArgumentNullException(nameof(options));

        if (string.IsNullOrWhiteSpace(_settings.AppDataPath))
        {
            throw new InvalidOperationException("AppSettings.AppDataPath must be configured");
        }
    }

    /// <summary>
    /// Asynchronously migrates the database.
    /// </summary>
    public async Task MigrateDatabaseAsync()
    {
        try
        {
            _logger.LogInformation("Starting async database migration");

            // Ensure app data directory exists
            try
            {
                if (!Directory.Exists(_settings.AppDataPath))
                {
                    Directory.CreateDirectory(_settings.AppDataPath);
                    _logger.LogInformation("Created app data directory: {AppDataPath}", _settings.AppDataPath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create app data directory: {AppDataPath}", _settings.AppDataPath);
                throw;
            }

            var dbPath = Path.Combine(_settings.AppDataPath, Database.DB_FILE_NAME);
            var dbExists = File.Exists(dbPath);

            // Run migrations asynchronously
            await using (var database = new Database())
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