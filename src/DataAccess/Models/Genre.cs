using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace PCRadio.DataAccess.Models;

public class Genre
{
    [Key]
    [ReadOnly(true)]
    [Browsable(false)]
    public int Id { get; set; }

    [Required]
    [StringLength(250, MinimumLength = 3)]
    [DisplayName("GenreName")]
    public required string Name { get; set; }

    [Browsable(false)]
    public virtual List<SubGenre>? SubGenres { get; set; }

    [Browsable(false)]
    public virtual List<StationGenre>? StationGenres { get; set; }
}
