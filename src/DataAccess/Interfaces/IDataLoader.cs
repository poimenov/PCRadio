using PCRadio.Services.Interfaces;

namespace PCRadio.DataAccess.Interfaces;

public interface IDataLoader
{
    Task LoadDataAsync(ParseResult result);
}
