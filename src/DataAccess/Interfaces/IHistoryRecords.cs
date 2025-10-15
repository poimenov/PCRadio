using PCRadio.DataAccess.Models;

namespace PCRadio.DataAccess.Interfaces;

public interface IHistoryRecords
{
    Task<IEnumerable<HistoryRecord>> GetAllAsync();
    Task AddAsync(HistoryRecord record);
}
