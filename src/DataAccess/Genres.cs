using Microsoft.EntityFrameworkCore;
using PCRadio.DataAccess.Interfaces;
using PCRadio.DataAccess.Models;

namespace PCRadio.DataAccess;

public class Genres : IGenres
{
    public IEnumerable<Genre> GetAll()
    {
        using (var db = new Database())
        {
            return db.Genres.Include(g => g.SubGenres).ToList();
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
