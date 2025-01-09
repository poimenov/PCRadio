using Microsoft.EntityFrameworkCore;
using PCRadio.DataAccess.Models;

namespace PCRadio.DataAccess;

//dotnet ef migrations add InitialCreate -o DataAccess/Migrations
public class Database : DbContext
{
    public const string DB_FILE_NAME = "PCRadio.db";

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        options.UseSqlite($"Data Source={DB_FILE_NAME}");
    }

    public virtual DbSet<Country> Countries { get; set; }
    public virtual DbSet<City> Cities { get; set; }
    public virtual DbSet<Genre> Genres { get; set; }
    public virtual DbSet<SubGenre> SubGenres { get; set; }
    public virtual DbSet<Station> Stations { get; set; }
    public virtual DbSet<StationCity> StationCities { get; set; }
    public virtual DbSet<StationGenre> StationGenres { get; set; }
    public virtual DbSet<StationSubGenre> StationSubGenres { get; set; }
}
