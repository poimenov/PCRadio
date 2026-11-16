using System.Text.Json.Serialization;

namespace PCRadio.Services.Models;

public sealed class StationTopic
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("slug")]
    public string Slug { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("short_description")]
    public string ShortDescription { get; set; } = string.Empty;

    [JsonPropertyName("image_url")]
    public string ImageUrl { get; set; } = string.Empty;

    [JsonPropertyName("station_count")]
    public int StationCount { get; set; }
}
