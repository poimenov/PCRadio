using PCRadio.DataAccess.Interfaces;
using PCRadio.Services.Models;

namespace PCRadio.Services.Interfaces;

public interface IStationTopicsService
{
    Task<IReadOnlyList<StationTopic>> GetTopicsAsync();
    Task<StationTopic?> GetTopicBySlugAsync(string slug);
    Task<StationTopicStationsResult> GetByTopicAsync(string slug, int skip, int take);
    Task<IEnumerable<StationInfo>> ExportByTopicAsync(string slug);
}
