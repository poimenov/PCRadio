using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PCRadio.DataAccess.Models;

public class Station
{
    [Key]
    [ReadOnly(true)]
    [Browsable(false)]
    public int Id { get; set; }

    [ReadOnly(true)]
    [Browsable(false)]
    public int UId { get; set; }

    [Required]
    [StringLength(250, MinimumLength = 3)]
    [DisplayName("StationName")]
    public required string Name { get; set; }

    [StringLength(1000)]
    [DisplayName("Description")]
    public string? Description { get; set; }

    [Required]
    [StringLength(150, MinimumLength = 15)]
    [DisplayName("Stream")]
    public required string Stream { get; set; }

    [Required]
    [StringLength(150, MinimumLength = 15)]
    [DisplayName("Logo")]
    public required string Logo { get; set; }

    [Required]
    [DefaultValue(false)]
    [DisplayName("Recomended")]
    public bool Recomended { get; set; }

    [DisplayName("Created")]
    public DateOnly? Created { get; set; }

    [ForeignKey(nameof(CountryId))]
    [Browsable(false)]
    public int? CountryId { get; set; }

    [Required]
    [DisplayName("Country")]
    public Country? Country { get; set; }

    [DefaultValue(false)]
    [DisplayName("IsFavorite")]
    public bool IsFavorite { get; set; }

    [Browsable(false)]
    public virtual List<StationCity>? StationCities { get; set; }

    [Browsable(false)]
    public virtual List<StationGenre>? StationGenres { get; set; }

    [Browsable(false)]
    public virtual List<StationSubGenre>? StationSubGenres { get; set; }
}
