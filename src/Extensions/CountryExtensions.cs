using Microsoft.AspNetCore.Components;
using _models = PCRadio.DataAccess.Models;

namespace PCRadio.Extensions;

public static class CountryExtensions
{
    public static MarkupString GetCountryIcon(this _models.Country country, bool withTitle = true)
    {
        return AppCultures.GetCountrySvgMarkupString(country.Id, withTitle);
    }
}
