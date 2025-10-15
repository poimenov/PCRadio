using PCRadio.Components.Pages;
using PCRadio.DataAccess.Models;

namespace PCRadio.Services.Interfaces;

public interface IAppStateService
{
    string Title { get; set; }
    int CurrentStationId { get; set; }
    HistoryRecord? LastHistoryRecord { get; set; }
    event Action<string>? TitleChanged;
    event Action<int>? CurrentStationIdChanged;
    event Action<HistoryRecord>? HistoryRecordChanged;
}
