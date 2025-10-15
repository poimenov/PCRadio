using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PCRadio.DataAccess.Models;

public class HistoryRecord
{
    [Key]
    [ReadOnly(true)]
    public DateTime StartTime { get; set; }

    [Required]
    [StringLength(500, MinimumLength = 3)]
    [DisplayName("StationName")]
    public required string TrackName { get; set; }

    [Required]
    [StringLength(250, MinimumLength = 3)]
    [DisplayName("StationName")]
    public required string StationName { get; set; }
}

public class HistoryRecordEqualityComparer : IEqualityComparer<HistoryRecord>
{
    public bool Equals(HistoryRecord? x, HistoryRecord? y)
    {
        if (x == null && y == null) return true;
        if (x == null || y == null) return false;
        return x.StartTime == y.StartTime
            && x.TrackName == y.TrackName
            && x.StationName == y.StationName;
    }

    public int GetHashCode(HistoryRecord obj)
    {
        return HashCode.Combine(obj.StartTime, obj.TrackName, obj.StationName);
    }
}
