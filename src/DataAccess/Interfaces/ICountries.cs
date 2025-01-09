using PCRadio.DataAccess.Models;

namespace PCRadio.DataAccess.Interfaces;

public interface ICountries
{
    IEnumerable<Country> GetAll();
}
