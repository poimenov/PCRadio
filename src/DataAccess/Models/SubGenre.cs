using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PCRadio.DataAccess.Models;

public class SubGenre
{
    [Key]
    [ReadOnly(true)]
    [Browsable(false)]
    public int Id { get; set; }

    [Required]
    [StringLength(250, MinimumLength = 3)]
    [DisplayName("SubGenreName")]
    public required string Name { get; set; }

    [Required]
    [ForeignKey(nameof(GenreId))]
    [Browsable(false)]
    public int GenreId { get; set; }

    [Required]
    [DisplayName("Genre")]
    public required Genre Genre { get; set; }

    [Browsable(false)]
    public virtual List<StationSubGenre>? StationSubGenres { get; set; }
}
