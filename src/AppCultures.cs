using System.Globalization;

namespace PCRadio;

public static class AppCultures
{
    public static CultureInfo[] Cultures => (new string[] { DefaultCulture.ToString(), "ru-RU" }).Select(x => new CultureInfo(x)).ToArray();
    public static CultureInfo DefaultCulture => new CultureInfo("en-US");

}


