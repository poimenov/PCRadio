using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PCRadio.DataAccess.Interfaces;
using PCRadio.DataAccess.Models;

namespace PCRadio.DataAccess;

public class Genres : IGenres
{
    private readonly ILogger<Genres> _logger;

    public Genres(ILogger<Genres> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public IEnumerable<Genre> GetAll()
    {
        try
        {
            using (var db = new Database())
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
        using (var db = new Database())
        {
            return db.SubGenres.Include(sg => sg.Genre).ToList();
        }
    }

    public Genre? GetById(int id)
    {
        using (var db = new Database())
        {
            return db.Genres.FirstOrDefault(g => g.Id == id);
        }
    }
}
