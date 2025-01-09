using Microsoft.EntityFrameworkCore;
using PCRadio.DataAccess.Interfaces;
using PCRadio.DataAccess.Models;

namespace PCRadio.DataAccess;

public class Countries : ICountries
{
    public IEnumerable<Country> GetAll()
    {
        using (var db = new Database())
        {
            return db.Countries.Include(c => c.Cities).ToList();
        }
    }
}
