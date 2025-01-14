using System.Text;
using Microsoft.AspNetCore.Components;
using PCRadio.DataAccess.Models;
using PSC.CSharp.Library.CountryData;
using PSC.CSharp.Library.CountryData.SVGImages;

namespace PCRadio.Extensions;

public static class StationExtensions
{
	private static CountryHelper countryHelper = new CountryHelper();

	public static MarkupString GetCountryIcon(this Station station)
	{
		var sb = new StringBuilder();
		sb.AppendLine("<svg xmlns=\"http://www.w3.org/2000/svg\" xmlns:xlink=\"http://www.w3.org/1999/xlink\" class=\"country-flag-icon\" viewBox=\"0 0 640 480\">");
		if (station.CountryId.HasValue && AppCultures.CountryCodes.ContainsKey(station.CountryId.Value))
		{
			var countryCode = AppCultures.CountryCodes[station.CountryId.Value];
			sb.AppendLine($"<title>{countryHelper.GetNameByCountryCode(countryCode)}</title>");
			sb.AppendLine(countryHelper.GetFlagByCountryCode(countryCode, FlagType.Wide));
		}
		sb.AppendLine("</svg>");
		return new MarkupString(sb.ToString());
	}
}
