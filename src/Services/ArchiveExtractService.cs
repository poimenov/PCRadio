using ICSharpCode.SharpZipLib.Zip;
using Microsoft.Extensions.Logging;
using PCRadio.Services.Interfaces;

namespace PCRadio.Services;

public class ArchiveExtractService : IArchiveExtractService
{
    private readonly ILogger<ArchiveExtractService> _logger;

    public ArchiveExtractService(ILogger<ArchiveExtractService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<string> ExtractArchiveAsync(string archivePath, string archivePassword)
    {
        if (string.IsNullOrWhiteSpace(archivePath))
        {
            _logger.LogError("ExtractArchiveAsync called with empty archivePath");
            throw new ArgumentException("archivePath must not be null or empty", nameof(archivePath));
        }

        if (archivePassword == null)
        {
            _logger.LogWarning("ExtractArchiveAsync called with null archivePassword");
            archivePassword = string.Empty;
        }

        var fileInfo = new FileInfo(archivePath);
        if (!fileInfo.Exists)
        {
            _logger.LogError("Archive file not found: {ArchivePath}", archivePath);
            throw new FileNotFoundException("Archive file not found", archivePath);
        }

        var outputPath = fileInfo.FullName.Replace(fileInfo.Extension, ".json");
        var outputDirectory = Path.GetDirectoryName(outputPath);

        try
        {
            if (!string.IsNullOrEmpty(outputDirectory))
                Directory.CreateDirectory(outputDirectory);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create output directory: {OutputDirectory}", outputDirectory);
            throw;
        }

        bool jsonFileFound = false;

        try
        {
            _logger.LogInformation("Extracting archive: {ArchivePath}", archivePath);

            using (var fileStream = fileInfo.OpenRead())
            using (var zipStream = new ZipInputStream(fileStream))
            {
                zipStream.Password = archivePassword;
                ZipEntry entry;

                while ((entry = zipStream.GetNextEntry()) != null)
                {
                    if (entry.IsDirectory)
                        continue;

                    if (string.IsNullOrEmpty(entry.Name) || !entry.Name.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                        continue;

                    // Prevent path traversal attacks
                    var fileName = Path.GetFileName(entry.Name);
                    if (string.IsNullOrEmpty(fileName) || fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                    {
                        _logger.LogWarning("Skipping entry with invalid filename: {EntryName}", entry.Name);
                        continue;
                    }

                    jsonFileFound = true;

                    try
                    {
                        await using (var fileWriter = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, useAsync: true))
                        {
                            await zipStream.CopyToAsync(fileWriter);
                            await fileWriter.FlushAsync();
                        }

                        _logger.LogInformation("Successfully extracted JSON file: {OutputPath}", outputPath);
                        return outputPath;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to write extracted JSON file to: {OutputPath}", outputPath);
                        throw;
                    }
                }
            }

            if (!jsonFileFound)
            {
                _logger.LogError("No JSON file found in archive: {ArchivePath}", archivePath);
                throw new InvalidOperationException($"No JSON file found in archive: {archivePath}");
            }

            // Should not reach here
            return outputPath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting archive: {ArchivePath}", archivePath);

            // Clean up partial file if extraction failed
            try
            {
                if (File.Exists(outputPath))
                    File.Delete(outputPath);
            }
            catch (Exception cleanupEx)
            {
                _logger.LogWarning(cleanupEx, "Failed to delete partial output file: {OutputPath}", outputPath);
            }

            throw;
        }
    }
}
