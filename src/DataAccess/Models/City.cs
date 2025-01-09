using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PCRadio.DataAccess.Models;

public class City
{
    [Key]
    [ReadOnly(true)]
    [Browsable(false)]
    public int Id { get; set; }

    [Required]
    [StringLength(150, MinimumLength = 3)]
    [DisplayName("CityName")]
    public required string Name { get; set; }

    [Required]
    [ForeignKey(nameof(CountryId))]
    [Browsable(false)]
    public int CountryId { get; set; }

    [Required]
    [DisplayName("Country")]
    public required Country Country { get; set; }

    [Browsable(false)]
    public virtual List<StationCity>? StationCities { get; set; }
}
