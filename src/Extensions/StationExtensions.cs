using Microsoft.AspNetCore.Components;
using PCRadio.DataAccess.Models;

namespace PCRadio.Extensions;

public static class StationExtensions
{
	public static MarkupString GetCountryIcon(this Station station)
	{
		return AppCultures.GetCountrySvgMarkupString(station.CountryId);
	}
}
