using Microsoft.Extensions.Options;
using PCRadio.Services.Interfaces;
using Microsoft.Extensions.Logging;
using PCRadio.DataAccess.Interfaces;

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

    public DbUpdateService(IOptions<AppSettings> options, ILogger<DbUpdateService> logger, IFileDownloadService fileDownloadService,
        IArchiveExtractService archiveExtractService, IParseJsonService parseJsonService, IDataLoader dataLoader)
    {
        _settings = options.Value;
        _logger = logger;
        _fileDownloadService = fileDownloadService;
        _archiveExtractService = archiveExtractService;
        _parseJsonService = parseJsonService;
        _dataLoader = dataLoader;
    }

    public async Task UpdateDatabaseAsync()
    {
        if (string.IsNullOrEmpty(_settings.ArchiveUrl))
        {
            _logger.LogWarning("ArchiveUrl is not set.");
            return;
        }

        if (string.IsNullOrEmpty(_settings.ArchivePassword))
        {
            _logger.LogWarning("ArchivePassword is not set.");
            return;
        }

        var filePath = Path.Combine(_settings.AppDataPath, FILE_NAME);
        var culture = AppCultures.Cultures.FirstOrDefault(x => x.ToString() == _settings.DefaultLanguage);
        if (culture == null)
        {
            culture = AppCultures.DefaultCulture;
        }

        //download file
        await _fileDownloadService.DownloadFileAsync(string.Format(_settings.ArchiveUrl, culture.TwoLetterISOLanguageName), filePath);

        //extract file
        var jsonFilePath = await _archiveExtractService.ExtractArchiveAsync(filePath, _settings.ArchivePassword);

        ///parse json
        var parseResult = _parseJsonService.Parse(jsonFilePath);

        //load data
        await _dataLoader.LoadDataAsync(parseResult);
    }

}
