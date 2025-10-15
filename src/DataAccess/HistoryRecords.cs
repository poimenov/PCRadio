using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PCRadio.DataAccess.Interfaces;
using PCRadio.DataAccess.Models;

namespace PCRadio.DataAccess;

public class HistoryRecords : IHistoryRecords
{
    private readonly int _historyRecordsCount;
    public HistoryRecords(IOptions<AppSettings> options)
    {
        _historyRecordsCount = options.Value.HistoryRecordsCount;
    }
    public async Task<IEnumerable<HistoryRecord>> GetAllAsync()
    {
        await using (var db = new Database())
        {
            return await db.HistoryRecords.OrderByDescending(r => r.StartTime).ToListAsync();
        }
    }

    public async Task AddAsync(HistoryRecord record)
    {
        await using (var db = new Database())
        {
            var existingRecordsCount = await db.HistoryRecords.CountAsync();
            if (existingRecordsCount >= _historyRecordsCount)
            {
                var recordsToRemove = await db.HistoryRecords.OrderBy(r => r.StartTime).Take(existingRecordsCount - _historyRecordsCount + 1).ToListAsync();
                db.HistoryRecords.RemoveRange(recordsToRemove);
            }

            await db.HistoryRecords.AddAsync(record);
            await db.SaveChangesAsync();
        }
    }
}
