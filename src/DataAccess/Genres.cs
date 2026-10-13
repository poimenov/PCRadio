using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PCRadio.DataAccess.Interfaces;
using PCRadio.DataAccess.Models;

namespace PCRadio.DataAccess;

public class Genres : IGenres
{
    private readonly ILogger<Genres> _logger;
    private readonly string _databasePath;

    public Genres(ILogger<Genres> logger, IOptions<AppSettings> options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _databasePath = Database.GetDatabasePath(options.Value.AppDataPath);
    }

    public IEnumerable<Genre> GetAll()
    {
        try
        {
            using (var db = new Database(_databasePath))
            {
                return db.Genres.Include(g => g.SubGenres).ToList();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while fetching all genres.");
            return Enumerable.Empty<Genre>();
        }
    }

    public IEnumerable<SubGenre> GetAllSubGenres()
    {
        using (var db = new Database(_databasePath))
        {
            return db.SubGenres.Include(sg => sg.Genre).ToList();
        }
    }

    public Genre? GetById(int id)
    {
        using (var db = new Database(_databasePath))
        {
            return db.Genres.FirstOrDefault(g => g.Id == id);
        }
    }
}
