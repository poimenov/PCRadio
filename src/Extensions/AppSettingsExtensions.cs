using System.Reflection.Metadata.Ecma335;

namespace PCRadio.Extensions;

public static class AppSettingsExtensions
{
    public static string GetQuality(this AppSettings settings)
    {
        string retVal = string.Empty;
        switch (settings.Quality)
        {
            case Quality.Low:
                retVal = "low";
                break;
            case Quality.Medium:
                retVal = "med";
                break;
            case Quality.High:
                retVal = "hi";
                break;
        };

        return retVal;
    }
}
