using System.Drawing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.FluentUI.AspNetCore.Components;
using Photino.Blazor;
using PCRadio.Components;
using Microsoft.Extensions.Options;
using System.Globalization;
using PCRadio.Services;
using PCRadio.Services.Interfaces;
using PCRadio.DataAccess;
using PCRadio.DataAccess.Interfaces;
using log4net.Config;
using System.Diagnostics;

namespace PCRadio;

public class Program
{
    private const string DATA_DIRECTORY = "DATA_DIRECTORY";
    [STAThread]
    public static void Main(string[] args)
    {
        var builder = PhotinoBlazorAppBuilder.CreateDefault(args);

        // Add services to the container.
        builder.Services.AddFluentUIComponents();
        builder.Services.AddLogging(logging =>
        {
            logging.ClearProviders().AddLog4Net();
        });
        builder.RootComponents.Add<App>("app");

        //add json configuration
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

        builder.Services.AddScoped(sp => new HttpClient(new HttpClientHandler(), disposeHandler: true));
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
        builder.Services.AddSingleton<IAppStateService, AppStateService>();

        var app = builder.Build();

        var settings = app.Services.GetRequiredService<IOptions<AppSettings>>().Value;

        var dataDirectory = Environment.GetEnvironmentVariable(DATA_DIRECTORY);
        if (dataDirectory == null)
        {
            Environment.SetEnvironmentVariable(DATA_DIRECTORY, settings.AppDataPath);
        }

        var cilture = AppCultures.Cultures.FirstOrDefault(x => x.ToString() == settings.DefaultLanguage);
        CultureInfo.DefaultThreadCurrentCulture = (cilture != null) ? cilture : AppCultures.DefaultCulture;
        CultureInfo.DefaultThreadCurrentUICulture = (cilture != null) ? cilture : AppCultures.DefaultCulture;

        AppDomain.CurrentDomain.SetData("DataDirectory", settings.AppDataPath);
        XmlConfigurator.Configure(new FileInfo("log4net.config"));

        if (!File.Exists(Path.Combine(settings.AppDataPath, Database.DB_FILE_NAME)) || settings.NeedsDatabaseUpdate)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            app.Services.GetRequiredService<IDbUpdateService>().UpdateDatabaseAsync().Wait();
            stopwatch.Stop();
            Debug.WriteLine($"Database update took {stopwatch.ElapsedMilliseconds} ms");
            settings.NeedsDatabaseUpdate = false;
            settings.Save();
        }

        // customize window
        app.MainWindow
            .SetSize(new Size(900, 500))
            .SetIconFile("wwwroot/favicon.ico")
            .SetTitle(AppSettings.APPLICATION_NAME);

        AppDomain.CurrentDomain.UnhandledException += (sender, error) =>
            {
                var ex = (Exception)error.ExceptionObject;
                app.Services.GetRequiredService<ILogger<Program>>().LogError(ex, ex.Message);
                app.MainWindow.ShowMessage("Fatal exception", error.ExceptionObject.ToString());
            };

        app.Run();
    }
}