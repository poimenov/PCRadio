using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PCRadio.DataAccess.Models;

namespace PCRadio.DataAccess;

public class Database : DbContext
{
    public const string DB_FILE_NAME = "PCRadio.db";

    public Database() : this(GetDefaultDatabasePath())
    {
    }

    public Database(string databasePath) : base(CreateOptions(databasePath))
    {
    }

    public static string GetDatabasePath(string appDataPath)
    {
        if (string.IsNullOrWhiteSpace(appDataPath))
        {
            throw new ArgumentException("App data path cannot be null or whitespace.", nameof(appDataPath));
        }

        return Path.GetFullPath(Path.Combine(appDataPath, DB_FILE_NAME));
    }

    private static string GetDefaultDatabasePath()
    {
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            AppSettings.APPLICATION_NAME);
        return GetDatabasePath(appDataPath);
    }

    private static DbContextOptions<Database> CreateOptions(string databasePath)
    {
        if (string.IsNullOrWhiteSpace(databasePath))
        {
            throw new ArgumentException("Database path cannot be null or whitespace.", nameof(databasePath));
        }

        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = Path.GetFullPath(databasePath)
        }.ToString();

        return new DbContextOptionsBuilder<Database>()
            .UseSqlite(connectionString)
            .Options;
    }

    public virtual DbSet<Country> Countries { get; set; }
    public virtual DbSet<City> Cities { get; set; }
    public virtual DbSet<Genre> Genres { get; set; }
    public virtual DbSet<SubGenre> SubGenres { get; set; }
    public virtual DbSet<Station> Stations { get; set; }
    public virtual DbSet<StationCity> StationCities { get; set; }
    public virtual DbSet<StationGenre> StationGenres { get; set; }
    public virtual DbSet<StationSubGenre> StationSubGenres { get; set; }
    public virtual DbSet<HistoryRecord> HistoryRecords { get; set; }
}
