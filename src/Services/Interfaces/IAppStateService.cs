using PCRadio.Components.Pages;
using PCRadio.DataAccess.Models;

namespace PCRadio.Services.Interfaces;

public interface IAppStateService
{
    string Title { get; set; }
    int CurrentStationId { get; set; }
    FavoriteStation? FavoriteStation { get; set; }
    HistoryRecord? LastHistoryRecord { get; set; }
    event Action<string>? TitleChanged;
    event Action<int>? CurrentStationIdChanged;
    event Action<HistoryRecord>? HistoryRecordChanged;
    event Action<FavoriteStation>? FavoriteStationChanged;
}

public class FavoriteStation
{
    public FavoriteStation() { }
    public FavoriteStation(int stationId, bool isFavorite)
    {
        StationId = stationId;
        IsFavorite = isFavorite;
    }
    public int StationId { get; set; }
    public bool IsFavorite { get; set; }
}

public class FavoriteStationEqualityComparer : IEqualityComparer<FavoriteStation>
{
    public bool Equals(FavoriteStation? x, FavoriteStation? y)
    {
        if (x == null && y == null) return true;
        if (x == null || y == null) return false;
        return x.StationId == y.StationId
            && x.IsFavorite == y.IsFavorite;
    }

    public int GetHashCode(FavoriteStation obj)
    {
        return HashCode.Combine(obj.StationId, obj.IsFavorite);
    }
}