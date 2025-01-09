using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace PCRadio.DataAccess.Models;

public class Country
{
    [Key]
    [ReadOnly(true)]
    [Browsable(false)]
    public int Id { get; set; }

    [Required]
    [StringLength(150, MinimumLength = 3)]
    [DisplayName("CountryName")]
    public required string Name { get; set; }

    [Browsable(false)]
    public virtual List<City>? Cities { get; set; }

    [Browsable(false)]
    public virtual List<Station>? Stations { get; set; }
}
