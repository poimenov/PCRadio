using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PCRadio.DataAccess.Models;

public class StationCity
{
    [Key]
    [ReadOnly(true)]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Browsable(false)]
    public int Id { get; set; }

    [Required]
    [ForeignKey(nameof(StationId))]
    [Browsable(false)]
    public int StationId { get; set; }

    [Required]
    [DisplayName("Station")]
    public required Station Station { get; set; }

    [Required]
    [ForeignKey(nameof(CityId))]
    [Browsable(false)]
    public int CityId { get; set; }

    [Required]
    [DisplayName("City")]
    public required City City { get; set; }
}
