using Microsoft.AspNetCore.Components;
using PCRadio.DataAccess.Models;

namespace PCRadio.Extensions;

public static class StationExtensions
{
	public static MarkupString GetCountryIcon(this Station station, bool withTitle = true, string className = "country-flag-icon")
	{
		return AppCultures.GetCountrySvgMarkupString(station.CountryId, withTitle, className);
	}
}
