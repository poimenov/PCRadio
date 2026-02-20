namespace PCRadio.Services.Interfaces;

public interface IOpenDialogService
{
    string[] OpenFile(string? title = null, string? defaultPath = null, bool? multiSelect = null, (string, string[])[]? filters = null);
    string[] OpenFolder(string? title = null, string? defaultPath = null, bool? multiSelect = null);
}