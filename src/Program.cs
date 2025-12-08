using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using log4net.Config;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.FluentUI.AspNetCore.Components;
using Photino.Blazor;
using PCRadio.Components;
using PCRadio.DataAccess;
using PCRadio.DataAccess.Interfaces;
using PCRadio.Services;
using PCRadio.Services.Interfaces;

namespace PCRadio;

public class Program
{
    private const string DATA_DIRECTORY = "DATA_DIRECTORY";
    private const int HTTP_CLIENT_TIMEOUT_SECONDS = 300; // 5 minutes

    [STAThread]
    public static void Main(string[] args)
    {
        try
        {
            var builder = PhotinoBlazorAppBuilder.CreateDefault(args);

            // Add services to the container.
            builder.Services.AddFluentUIComponents();
            builder.Services.AddLogging(logging =>
            {
                logging.ClearProviders().AddLog4Net();
            });
            builder.RootComponents.Add<App>("app");

            // Add JSON configuration
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile(AppSettings.JSON_FILE_NAME, optional: true, reloadOnChange: false)
                .AddEnvironmentVariables()
                .Build();

            builder.Services.AddSingleton<IConfiguration>(configuration);
            builder.Services.Configure<AppSettings>(configuration);
            builder.Services.AddLocalization(options =>
            {
                options.ResourcesPath = "Resources";
            });

            // Configure HttpClient with timeout
            builder.Services.AddSingleton(sp =>
            {
                var handler = new HttpClientHandler();
                var client = new HttpClient(handler, disposeHandler: true)
                {
                    Timeout = TimeSpan.FromSeconds(HTTP_CLIENT_TIMEOUT_SECONDS)
                };
                return client;
            });

            // Register services
            builder.Services.AddTransient<IPlatformService, PlatformService>();
            builder.Services.AddTransient<IProcessService, ProcessService>();
            builder.Services.AddTransient<ILinkOpeningService, LinkOpeningService>();
            builder.Services.AddTransient<IFileDownloadService, FileDownloadService>();
            builder.Services.AddTransient<IArchiveExtractService, ArchiveExtractService>();
            builder.Services.AddTransient<IParseJsonService, ParseJsonService>();
            builder.Services.AddTransient<IDatabaseMigrator, DatabaseMigrator>();
            builder.Services.AddTransient<IDataLoader, DataLoader>();
            builder.Services.AddTransient<IDbUpdateService, DbUpdateService>();
            builder.Services.AddTransient<IStations, Stations>();
            builder.Services.AddTransient<IGenres, Genres>();
            builder.Services.AddTransient<ICountries, Countries>();
            builder.Services.AddTransient<IHistoryRecords, HistoryRecords>();
            builder.Services.AddSingleton<IAppStateService, AppStateService>();

            var app = builder.Build();

            var logger = app.Services.GetRequiredService<ILogger<Program>>();
            logger.LogInformation("Application starting...");

            // Get application settings
            var settings = app.Services.GetRequiredService<IOptions<AppSettings>>().Value;

            if (settings == null)
            {
                logger.LogCritical("AppSettings configuration is missing or invalid");
                throw new InvalidOperationException("AppSettings configuration is missing or invalid");
            }

            if (string.IsNullOrWhiteSpace(settings.AppDataPath))
            {
                logger.LogCritical("AppSettings.AppDataPath is not configured");
                throw new InvalidOperationException("AppSettings.AppDataPath must be configured");
            }

            // Ensure app data directory exists
            try
            {
                Directory.CreateDirectory(settings.AppDataPath);
                logger.LogInformation("App data directory ensured: {AppDataPath}", settings.AppDataPath);
            }
            catch (Exception ex)
            {
                logger.LogCritical(ex, "Failed to create app data directory: {AppDataPath}", settings.AppDataPath);
                throw;
            }

            // Set environment variable if not already set
            var existingDataDirectory = Environment.GetEnvironmentVariable(DATA_DIRECTORY);
            if (string.IsNullOrEmpty(existingDataDirectory))
            {
                Environment.SetEnvironmentVariable(DATA_DIRECTORY, settings.AppDataPath);
                logger.LogDebug("Environment variable {DataDirectoryVar} set to: {AppDataPath}", DATA_DIRECTORY, settings.AppDataPath);
            }

            // Set default culture
            var culture = AppCultures.Cultures.FirstOrDefault(x => x.ToString() == settings.DefaultLanguage)
                ?? AppCultures.DefaultCulture;

            if (culture.ToString() != settings.DefaultLanguage)
            {
                logger.LogInformation("Requested culture '{RequestedCulture}' not found, using default culture: {DefaultCulture}",
                    settings.DefaultLanguage, culture);
            }

            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;

            // Configure AppDomain and logging
            AppDomain.CurrentDomain.SetData("DataDirectory", settings.AppDataPath);
            var log4netConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "log4net.config");
            if (File.Exists(log4netConfigPath))
            {
                XmlConfigurator.Configure(new FileInfo(log4netConfigPath));
            }
            else
            {
                logger.LogWarning("log4net.config file not found at: {Path}", log4netConfigPath);
            }

            // Initialize or update database if needed
            var dbPath = Path.Combine(settings.AppDataPath, Database.DB_FILE_NAME);
            if (!File.Exists(dbPath) || settings.NeedsDatabaseUpdate)
            {
                logger.LogInformation("Database initialization/update required");
                InitializeDatabaseAsync(app, logger, settings, dbPath);
            }
            else
            {
                logger.LogInformation("Database already initialized and up to date");
            }

            // Customize window
            app.MainWindow
                .SetSize(new Size(900, 600))
                .SetIconFile("wwwroot/favicon.ico")
                .SetTitle(AppSettings.APPLICATION_NAME);

            // Handle unhandled exceptions
            AppDomain.CurrentDomain.UnhandledException += (sender, error) =>
            {
                var ex = (Exception)error.ExceptionObject;
                logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);
                app.MainWindow.ShowMessage("Fatal Exception", $"An unhandled exception occurred:\n\n{ex.Message}\n\nDetails:\n{ex}");
            };

            logger.LogInformation("Application started successfully");
            app.Run();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Fatal error during application startup: {ex.Message}");
            Console.Error.WriteLine(ex.StackTrace);
            Environment.Exit(1);
        }
    }

    private static void InitializeDatabaseAsync(PhotinoBlazorApp app, ILogger<Program> logger, AppSettings settings, string dbPath)
    {
        try
        {
            var stopwatch = Stopwatch.StartNew();
            logger.LogInformation("Starting database update/initialization...");

            var dbUpdateService = app.Services.GetRequiredService<IDbUpdateService>();
            var success = dbUpdateService.UpdateDatabaseAsync().GetAwaiter().GetResult();

            stopwatch.Stop();

            if (success)
            {
                logger.LogInformation("Database update completed successfully in {ElapsedMilliseconds}ms", stopwatch.ElapsedMilliseconds);
                settings.NeedsDatabaseUpdate = false;
            }
            else
            {
                logger.LogError("Database update failed after {ElapsedMilliseconds}ms", stopwatch.ElapsedMilliseconds);
                settings.NeedsDatabaseUpdate = true;
            }

            try
            {
                settings.Save();
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to save updated settings");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Critical error during database initialization");
            throw;
        }
    }
}