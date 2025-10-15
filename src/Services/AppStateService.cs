using PCRadio.DataAccess.Models;
using PCRadio.Services.Interfaces;

namespace PCRadio.Services;

public class AppStateService : IAppStateService
{
    private HistoryRecordEqualityComparer historyRecordEqualityComparer = new HistoryRecordEqualityComparer();
    private FavoriteStationEqualityComparer favoriteStationEqualityComparer = new FavoriteStationEqualityComparer();
    private FavoriteStation? _favoriteStation;
    public FavoriteStation? FavoriteStation
    {
        get => _favoriteStation;
        set
        {
            if (!favoriteStationEqualityComparer.Equals(_favoriteStation, value))
            {
                _favoriteStation = value;
                if (value != null)
                {
                    FavoriteStationChanged?.Invoke(value);
                }
            }
        }
    }
    private HistoryRecord? _lastHistoryRecord;
    public HistoryRecord? LastHistoryRecord
    {
        get => _lastHistoryRecord;
        set
        {
            if (!historyRecordEqualityComparer.Equals(_lastHistoryRecord, value))
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
    public event Action<FavoriteStation>? FavoriteStationChanged;
}
