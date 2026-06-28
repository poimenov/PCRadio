using PCRadio.DataAccess.Models;

namespace PCRadio.DataAccess.Interfaces;

public interface IGenres
{
    IEnumerable<Genre> GetAll();
    IEnumerable<SubGenre> GetAllSubGenres();
    Genre? GetById(int id);
}
