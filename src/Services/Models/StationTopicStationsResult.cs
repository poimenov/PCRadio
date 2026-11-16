using PCRadio.DataAccess.Interfaces;

namespace PCRadio.Services.Models;

public sealed class StationTopicStationsResult : StationsResult
{
    public string TopicName { get; set; } = string.Empty;
}
