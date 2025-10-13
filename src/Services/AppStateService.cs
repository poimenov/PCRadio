using PCRadio.DataAccess.Models;

namespace PCRadio.Services;

public class AppStateService : Interfaces.IAppStateService
{
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
}
