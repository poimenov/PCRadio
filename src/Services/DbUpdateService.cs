using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PCRadio.DataAccess.Interfaces;
using PCRadio.Services.Interfaces;

namespace PCRadio.Services;

public class DbUpdateService : IDbUpdateService
{
    private const string FILE_NAME = "pcradio.zip";
    private readonly AppSettings _settings;
    private readonly ILogger<DbUpdateService> _logger;
    private readonly IFileDownloadService _fileDownloadService;
    private readonly IArchiveExtractService _archiveExtractService;
    private readonly IParseJsonService _parseJsonService;
    private readonly IDataLoader _dataLoader;

    public DbUpdateService(
        IOptions<AppSettings> options,
        ILogger<DbUpdateService> logger,
        IFileDownloadService fileDownloadService,
        IArchiveExtractService archiveExtractService,
        IParseJsonService parseJsonService,
        IDataLoader dataLoader)
    {
        _settings = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _fileDownloadService = fileDownloadService ?? throw new ArgumentNullException(nameof(fileDownloadService));
        _archiveExtractService = archiveExtractService ?? throw new ArgumentNullException(nameof(archiveExtractService));
        _parseJsonService = parseJsonService ?? throw new ArgumentNullException(nameof(parseJsonService));
        _dataLoader = dataLoader ?? throw new ArgumentNullException(nameof(dataLoader));
    }

    public async Task<bool> UpdateDatabaseAsync()
    {
        // Validate settings
        if (string.IsNullOrWhiteSpace(_settings.ArchiveUrl))
        {
            _logger.LogWarning("ArchiveUrl is not set");
            return false;
        }

        if (string.IsNullOrWhiteSpace(_settings.ArchivePassword))
        {
            _logger.LogWarning("ArchivePassword is not set");
        }

        if (string.IsNullOrWhiteSpace(_settings.AppDataPath))
        {
            _logger.LogWarning("AppDataPath is not set");
            return false;
        }

        try
        {
            // Ensure app data directory exists
            Directory.CreateDirectory(_settings.AppDataPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create AppDataPath directory: {Path}", _settings.AppDataPath);
            return false;
        }

        // Select language/culture
        var culture = AppCultures.Cultures.FirstOrDefault(x => x.ToString() == _settings.DefaultLanguage);
        if (culture == null)
        {
            _logger.LogInformation("Language {Language} not found, using default culture {DefaultCulture}",
                _settings.DefaultLanguage, AppCultures.DefaultCulture);
            culture = AppCultures.DefaultCulture;
        }

        var filePath = Path.Combine(_settings.AppDataPath, FILE_NAME);
        string jsonFilePath = string.Empty;

        try
        {
            // Download archive file
            _logger.LogInformation("Downloading archive from: {Url}", _settings.ArchiveUrl);
            var downloadUrl = string.Format(_settings.ArchiveUrl, culture.TwoLetterISOLanguageName);

            if (!await _fileDownloadService.DownloadFileAsync(downloadUrl, filePath))
            {
                _logger.LogError("Failed to download archive file from: {Url}", downloadUrl);
                return false;
            }

            _logger.LogInformation("Archive downloaded successfully: {Path}", filePath);

            // Extract archive
            _logger.LogInformation("Extracting archive: {Path}", filePath);
            jsonFilePath = await _archiveExtractService.ExtractArchiveAsync(filePath, _settings.ArchivePassword ?? string.Empty);

            if (string.IsNullOrWhiteSpace(jsonFilePath) || !File.Exists(jsonFilePath))
            {
                _logger.LogError("Failed to extract archive or JSON file not found");
                return false;
            }

            _logger.LogInformation("Archive extracted successfully: {JsonPath}", jsonFilePath);

            // Parse JSON
            _logger.LogInformation("Parsing JSON: {JsonPath}", jsonFilePath);
            var parseResult = await _parseJsonService.ParseAsync(jsonFilePath);

            if (parseResult == null)
            {
                _logger.LogError("Failed to parse JSON file");
                return false;
            }

            _logger.LogInformation("JSON parsed successfully. Stations: {StationCount}", parseResult.Stations?.Count ?? 0);

            // Load data into database
            _logger.LogInformation("Loading data into database");
            var result = await _dataLoader.LoadDataAsync(parseResult);

            if (result)
            {
                _logger.LogInformation("Database updated successfully");
            }
            else
            {
                _logger.LogError("Failed to load data into database");
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during database update");
            return false;
        }
        finally
        {
            // Clean up temporary files
            try
            {
                if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
                {
                    File.Delete(filePath);
                    _logger.LogDebug("Deleted temporary archive file: {Path}", filePath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete archive file: {Path}", filePath);
            }

            try
            {
                if (!string.IsNullOrEmpty(jsonFilePath) && File.Exists(jsonFilePath))
                {
                    File.Delete(jsonFilePath);
                    _logger.LogDebug("Deleted temporary JSON file: {Path}", jsonFilePath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete JSON file: {Path}", jsonFilePath);
            }
        }
    }
}
