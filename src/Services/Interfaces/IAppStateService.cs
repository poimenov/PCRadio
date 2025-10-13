namespace PCRadio.Services.Interfaces;

public interface IAppStateService
{
    string Title { get; set; }
    int CurrentStationId { get; set; }
    event Action<string>? TitleChanged;
    event Action<int>? CurrentStationIdChanged;
}
