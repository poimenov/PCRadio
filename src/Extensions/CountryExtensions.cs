using System.Text;
using Microsoft.AspNetCore.Components;
using _models = PCRadio.DataAccess.Models;
using PSC.CSharp.Library.CountryData;
using PSC.CSharp.Library.CountryData.SVGImages;

namespace PCRadio.Extensions;

public static class CountryExtensions
{
    private static CountryHelper countryHelper = new CountryHelper();

    public static MarkupString GetCountryIcon(this _models.Country country)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<svg xmlns=\"http://www.w3.org/2000/svg\" xmlns:xlink=\"http://www.w3.org/1999/xlink\" class=\"country-flag-icon\" viewBox=\"0 0 640 480\">");
        if (AppCultures.CountryCodes.ContainsKey(country.Id))
        {
            var countryCode = AppCultures.CountryCodes[country.Id];
            sb.AppendLine($"<title>{country.Name}</title>");
            sb.AppendLine(countryHelper.GetFlagByCountryCode(countryCode, FlagType.Wide));
        }
        sb.AppendLine("</svg>");
        return new MarkupString(sb.ToString());
    }
}
