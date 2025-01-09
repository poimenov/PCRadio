using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PCRadio.DataAccess.Models;

public class StationSubGenre
{
    [Key]
    [ReadOnly(true)]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Browsable(false)]
    public int Id { get; set; }

    [ForeignKey(nameof(StationId))]
    [Browsable(false)]
    public int StationId { get; set; }

    [Required]
    [DisplayName("Station")]
    public required Station Station { get; set; }

    [ForeignKey(nameof(SubGenreId))]
    [Browsable(false)]
    public int SubGenreId { get; set; }

    [Required]
    [DisplayName("SubGenre")]
    public required SubGenre SubGenre { get; set; }
}
