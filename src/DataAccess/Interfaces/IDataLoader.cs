using PCRadio.Services.Interfaces;

namespace PCRadio.DataAccess.Interfaces;

public interface IDataLoader
{
    Task<bool> LoadDataAsync(ParseResult result);
}
