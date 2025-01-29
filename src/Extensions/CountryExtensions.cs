using Microsoft.AspNetCore.Components;
using _models = PCRadio.DataAccess.Models;
using PSC.CSharp.Library.CountryData;

namespace PCRadio.Extensions;

public static class CountryExtensions
{
    private static CountryHelper countryHelper = new CountryHelper();

    public static MarkupString GetCountryIcon(this _models.Country country, bool withTitle = true)
    {
        return AppCultures.GetCountrySvgMarkupString(country.Id, withTitle);
    }
}
