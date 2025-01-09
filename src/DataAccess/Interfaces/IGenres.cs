using PCRadio.DataAccess.Models;

namespace PCRadio.DataAccess.Interfaces;

public interface IGenres
{
    IEnumerable<Genre> GetAll();
    Genre? GetById(int id);
}
