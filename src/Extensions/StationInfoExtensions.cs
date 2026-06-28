using Microsoft.AspNetCore.Components;
using PCRadio.Services.Interfaces;
using RadioBrowser.Models;
namespace PCRadio.Extensions;

public static class StationInfoExtensions
{
    public static MarkupString GetCountryIcon(this StationInfo stationInfo)
    {
        return AppCultures.GetCountrySvgMarkupString(stationInfo.CountryCode);
    }
}

public static class CountryAndCountExtensions
{
    public static MarkupString GetCountryIcon(this CountryAndCount country, bool withTitle)
    {
        return AppCultures.GetCountrySvgMarkupString(country.Name.ToLower(), withTitle);
    }
}
