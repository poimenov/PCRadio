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

  public static Dictionary<string, int> GenresEn = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
  {
    { "Rock", 1 },
    { "Pop", 2 },
    { "Metal", 25 },
    { "News", 28 },
    { "Talk", 28 },
    { "Dance", 30 },
    { "Adult Contemporary", 35 },
    { "Electronic", 36 },
    { "Humor", 37 },
    { "Relax", 38 },
    { "Easy listening", 38 },
    { "Russian shanson", 39 },
    { "Rap", 40 },
    { "Hip-hop", 40 },
    { "RnB", 40 },
    { "Ska", 41 },
    { "Rocksteady", 41 },
    { "Reggae", 41 },
    { "Country", 42 },
    { "Americana", 42 },
    { "Bluegrass", 42 },
    { "Jazz", 43 },
    { "Blues", 43 },
    { "Soul", 43 },
    { "Classical", 44 },
    { "Past decades", 45 },
    { "Children's", 46 },
    { "Religion", 47 },
    { "Folk, Ethnic", 48 },
    { "Discography", 49 },
    { "Audiobooks", 50 },
    { "Game", 51 },
    { "Anime", 51 },
    { "Yoga", 52 },
    { "Spa", 52 },
    { "Meditation", 52 },
    { "Indie", 53 },
    { "Misc", 54 },
    { "Quality test", 55 },
    { "Funk", 57 },
    { "Disco", 57 },
    { "Sports", 59 },
    { "Tech massage", 60 }
  };

  public static Dictionary<string, int> SubGenresEn = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
  {
    { "Classic Rock", 1 },
    { "Euro Pop", 2 },
    { "Top40", 5 },
    { "Russian Pop", 23 },
    { "Alternative rock", 24 },
    { "House", 25 },
    { "Trance", 26 },
    { "Dubstep", 27 },
    { "Techno", 28 },
    { "Drum-n-bass", 29 },
    { "Smooth Jazz", 30 },
    { "Instrumental", 31 },
    { "Lounge", 32 },
    { "New Age", 33 },
    { "Ambient", 34 },
    { "Classical Hits", 35 },
    { "Opera", 36 },
    { "Oldies", 37 },
    { "60's", 38 },
    { "70's", 39 },
    { "80's", 40 },
    { "90's", 41 },
    { "2000's", 42 },
    { "Politics", 43 },
    { "Business", 45 },
    { "Economics", 45 },
    { "Community", 46 },
    { "Culture", 46 },
    { "Bollywood", 47 },
    { "Caucasus", 48 },
    { "Asia", 49 },
    { "Latin America", 50 },
    { "Exotic", 51 },
    { "Club", 53 },
    { "Vocals", 54 },
    { "Britpop", 55 },
    { "Russian Rock", 56 },
    { "Hardstyle", 57 },
    { "Chillout", 58 },
    { "Breakbeats", 59 },
    { "50's", 60 },
    { "OST", 61 },
    { "Live", 62 },
    { "Holidays", 63 },
    { "Love songs", 64 },
    { "Fitness", 65 },
    { "Gym music", 65 },
    { "Local news", 66 },
    { "Italo Disco", 67 },
    { "Synth Pop", 68 },
    { "Africa", 69 },
    { "Mid East", 70 },
    { "Educational", 71 },
    { "Science", 71 },
    { "Electro", 72 },
    { "Russian Folk", 73 },
    { "Classical crossover", 74 },
    { "Punk Rock", 75 },
    { "French music", 76 },
    { "University", 77 },
    { "Travel", 78 },
    { "Sci-Fi", 79 },
    { "Fantasy", 79 },
    { "Fairy Tales", 80 },
    { "Books for kids", 80 },
    { "Classic literature", 81 },
    { "Detective story", 83 },
    { "Thriller", 83 },
    { "Celtic", 84 },
    { "Nu Disco", 85 },
    { "Nu Jazz", 85 },
    { "Progressive Rock", 86 },
    { "Trap", 87 },
    { "Classic hits", 88 },
    { "Sounds of Nature", 89 },
    { "Christianity", 90 },
    { "Islam", 91 },
    { "Other", 92 },
    { "Religious music", 93 },
    { "Judaism", 94 },
    { "Italian", 95 },
    { "German", 96 },
    { "Buddhism", 97 },
    { "Hinduism", 98 },
    { "Block redirect", 99 },
    { "Technical work", 100 },
    { "Play soon", 101 },
    { "Tamil", 102 },
    { "Grunge", 103 }
  };

  public static MarkupString GetCountrySvgMarkupString(int? id, bool withTitle = true, string className = "country-flag-icon")
  {
    var countryCode = string.Empty;
    if (id.HasValue && CountryCodes.ContainsKey(id.Value))
    {
      countryCode = CountryCodes[id.Value];
    }
    return GetCountrySvgMarkupString(countryCode, withTitle, className);
  }

  public static MarkupString GetCountrySvgMarkupString(string? countryCode, bool withTitle = true, string className = "country-flag-icon")
  {
    var sb = new StringBuilder();
    sb.AppendLine($"<svg xmlns=\"http://www.w3.org/2000/svg\" xmlns:xlink=\"http://www.w3.org/1999/xlink\" class=\"{className}\" viewBox=\"0 0 640 480\">");
    if (!string.IsNullOrEmpty(countryCode))
    {
      if (withTitle)
      {
        sb.AppendLine($"<title>{countryHelper.GetNameByCountryCode(countryCode.ToLower())}</title>");
      }

      sb.AppendLine(countryHelper.GetFlagByCountryCode(countryCode.ToLower(), FlagType.Wide));
    }
    sb.AppendLine("</svg>");
    if (!string.IsNullOrEmpty(countryCode) && countryCode.ToLower() == "de")
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

