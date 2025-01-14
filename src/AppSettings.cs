using System.Text.Json;

namespace PCRadio;

public enum Quality
{
    Low,
    Medium,
    High
}

public class AppSettings
{
    public const string APPLICATION_NAME = "PCRadio";
    public const string JSON_FILE_NAME = "appsettings.json";

    public string? DefaultLanguage { get; set; }
    public string? ArchiveUrl { get; set; }
    public string? ArchivePassword { get; set; }
    public int PageSize { get; set; } = 50;
    public Quality Quality { get; set; } = Quality.Medium;
    private string _appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), APPLICATION_NAME);
    public string AppDataPath
    {
        get => _appDataPath;
        set => _appDataPath = value;
    }
    public void Save()
    {
        string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, JSON_FILE_NAME);
        if (File.Exists(filePath))
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            string jsonString = JsonSerializer.Serialize(this, options);
            File.WriteAllText(filePath, jsonString);
        }
    }
}
