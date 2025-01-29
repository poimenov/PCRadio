using Microsoft.AspNetCore.Components;
using PCRadio.DataAccess.Models;
using PSC.CSharp.Library.CountryData;

namespace PCRadio.Extensions;

public static class StationExtensions
{
	private static CountryHelper countryHelper = new CountryHelper();

	public static MarkupString GetCountryIcon(this Station station)
	{
		return AppCultures.GetCountrySvgMarkupString(station.CountryId);
	}
}
