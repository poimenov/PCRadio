
using PCRadio.Services.Interfaces;
namespace PCRadio.Services;

public class OpenDialogService : IOpenDialogService
{
    private readonly Photino.Blazor.PhotinoBlazorApp app;

    public OpenDialogService(Photino.Blazor.PhotinoBlazorApp app)
    {
        this.app = app;
    }

    public string[] OpenFile(string? title = null, string? defaultPath = null, bool? multiSelect = null, (string, string[])[]? filters = null)
    {
        return app.MainWindow.ShowOpenFile(
            title,
            defaultPath,
            multiSelect ?? false,
            filters
        );
    }

    public string[] OpenFolder(string? title = null, string? defaultPath = null, bool? multiSelect = null)
    {
        return app.MainWindow.ShowOpenFolder(
            title,
            defaultPath,
            multiSelect ?? false
        );
    }
}