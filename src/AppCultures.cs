using System.Globalization;
using System.Text;
using System.Xml.Linq;
using Microsoft.AspNetCore.Components;
using PSC.CSharp.Library.CountryData;
using PSC.CSharp.Library.CountryData.SVGImages;

namespace PCRadio;

public static class AppCultures
{
    private static CountryHelper countryHelper = new CountryHelper();
    public static CultureInfo[] Cultures => (new string[] { DefaultCulture.ToString(), "ru-RU" }).Select(x => new CultureInfo(x)).ToArray();
    public static CultureInfo DefaultCulture => new CultureInfo("en-US");
    public static Dictionary<int, string> CountryCodes = new Dictionary<int, string>()
    {
        {1, "ru"},
        {2, "ua"},
        {3, "us"},
        {4, "de"},
        {5, "by"},
        {6, "fr"},
        {7, "au"},
        {8, "at"},
        {9, "il"},
        {10, "lt"},
        {11, "pl"},
        {12, "gb"},
        {13, "lb"},
        {14, "es"},
        {15, "pt"},
        {16, "it"},
        {17, "kg"},
        {18, "kz"},
        {19, "az"},
        {20, "br"},
        {21, "ae"},
        {22, "nl"},
        {23, "ma"},
        {24, "ro"},
        {25, "ca"},
        {26, "ee"},
        {27, "dk"},
        {28, "ar"},
        {29, "gr"},
        {30, "tr"},
        {31, "hr"},
        {32, "kp"},
        {33, "uz"},
        {34, "rs"},
        {35, "hu"},
        {36, "lv"},
        {37, "fi"},
        {38, "md"},
        {39, "cz"},
        {40, "jp"},
        {41, "be"},
        {42, "ch"},
        {43, "in"},
        {44, "bg"},
        {45, "tj"},
        {46, "sa"},
        {47, "cl"},
        {48, "sg"},
        {49, "mx"},
        {50, "am"},
        {51, "cr"},
        {52, "nz"},
        {53, "se"},
        {54, "co"},
        {55, "cy"},
        {56, "th"},
        {57, "mk"},
        {58, "ie"},
        {59, "kr"},
        {60, "ge"},
        {61, "no"},
        {62, "cn"},
        {63, "pe"},
        {64, "sy"},
        {65, "za"},
        {66, "hn"},
        {67, "uy"},
        {68, "bb"},
        {69, "sk"},
        {71, "tw"},
        {72, "id"},
        {73, "bz"},
        {74, "gh"},
        {75, "ao"},
        {76, "do"},
        {77, "py"},
        {78, "gt"},
        {79, "si"},
        {80, "hk"},
        {81, "sc"},
        {82, "is"},
        {83, "gi"},
        {84, "mt"},
        {85, "ng"},
        {86, "ba"},
        {87, "ec"},
        {88, "tn"},
        {89, "ir"},
        {90, "jm"},
        {91, "pk"},
        {92, "lu"},
        {93, "lk"},
        {94, "kw"},
        {95, "cu"},
        {96, "my"},
        {97, "bo"},
        {98, "ve"},
        {99, "na"},
        {100, "mu"},
        {101, "mz"},
        {102, "im"},
        {103, "bd"},
        {104, "tt"},
        {105, "pa"},
        {106, "mn"},
        {107, "ph"},
        {108, "ht"},
        {110, "dz"},
        {111, "al"},
        {112, "zm"},
        {113, "mc"},
        {114, "tz"}
    };

    public static MarkupString GetCountrySvgMarkupString(int? id, bool withTitle = true)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<svg xmlns=\"http://www.w3.org/2000/svg\" xmlns:xlink=\"http://www.w3.org/1999/xlink\" class=\"country-flag-icon\" viewBox=\"0 0 640 480\">");
        if (id.HasValue && CountryCodes.ContainsKey(id.Value))
        {
            var countryCode = CountryCodes[id.Value];
            if (withTitle)
            {
                sb.AppendLine($"<title>{countryHelper.GetNameByCountryCode(countryCode)}</title>");
            }

            sb.AppendLine(countryHelper.GetFlagByCountryCode(countryCode, FlagType.Wide));
        }
        sb.AppendLine("</svg>");

        if (id.HasValue && id.Value == 4)
        {
            var svg = XElement.Parse(sb.ToString());
            var pathes = svg.Descendants().Where(x => x.Name.LocalName == "path" && !x.Attributes().Any(y => y.Name == "fill"));
            foreach (var path in pathes)
            {
                path.SetAttributeValue("fill", "black");
            }

            return new MarkupString(svg.ToString());
        }

        return new MarkupString(sb.ToString());
    }
}


