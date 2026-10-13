using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PCRadio.DataAccess.Interfaces;
using PCRadio.DataAccess.Models;

namespace PCRadio.DataAccess;

public class Countries : ICountries
{
    private readonly string _databasePath;

    public Countries(IOptions<AppSettings> options)
    {
        _databasePath = Database.GetDatabasePath(options.Value.AppDataPath);
    }

    public IEnumerable<Country> GetAll()
    {
        using (var db = new Database(_databasePath))
        {
            return db.Countries.Include(c => c.Cities).ToList();
        }
    }
}
