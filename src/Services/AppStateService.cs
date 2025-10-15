using PCRadio.DataAccess.Models;
using PCRadio.Services.Interfaces;

namespace PCRadio.Services;

public class AppStateService : IAppStateService
{
    private HistoryRecord? _lastHistoryRecord;
    public HistoryRecord? LastHistoryRecord
    {
        get => _lastHistoryRecord;
        set
        {
            if (_lastHistoryRecord != value)
            {
                _lastHistoryRecord = value;
                if (value != null)
                {
                    HistoryRecordChanged?.Invoke(value);
                }
            }
        }
    }

    private string _title = string.Empty;
    public string Title
    {
        get => _title;
        set
        {
            if (_title != value)
            {
                _title = value;
                TitleChanged?.Invoke(_title);
            }
        }
    }

    private int _currentStationId;
    public int CurrentStationId
    {
        get => _currentStationId;
        set
        {
            if (_currentStationId != value)
            {
                _currentStationId = value;
                CurrentStationIdChanged?.Invoke(_currentStationId);
            }
        }
    }


    public event Action<string>? TitleChanged;
    public event Action<int>? CurrentStationIdChanged;
    public event Action<HistoryRecord>? HistoryRecordChanged;
}
