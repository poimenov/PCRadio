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
        _logger = logger;
        _settings = options.Value;
    }    
    public void MigrateDatabase()
    {
        if (!Directory.Exists(_settings.AppDataPath))
        {
            Directory.CreateDirectory(_settings.AppDataPath);
        }

        var path = Path.Combine(_settings.AppDataPath, Database.DB_FILE_NAME);
        if (!File.Exists(path))
        {
            using (var _database = new Database())
            {
                _database.Database.Migrate();
            }

            if (_logger != null)
            {
                _logger.LogInformation("Database created");
            }
        }
    }
}
