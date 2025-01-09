namespace PCRadio.Services.Interfaces;

public interface IArchiveExtractService
{
    Task<string> ExtractArchiveAsync(string archivePath, string archivePassword);
}
