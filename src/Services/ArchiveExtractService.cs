using ICSharpCode.SharpZipLib.Zip;
using Microsoft.Extensions.Logging;
using PCRadio.Services.Interfaces;

namespace PCRadio.Services;

public class ArchiveExtractService : IArchiveExtractService
{
    private const long MAX_ARCHIVE_SIZE_BYTES = 25L * 1024 * 1024;
    private const long MAX_EXPANDED_SIZE_BYTES = 512L * 1024 * 1024;
    private const long MAX_JSON_SIZE_BYTES = 256L * 1024 * 1024;
    private const int MAX_ARCHIVE_ENTRIES = 10_000;
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

        if (fileInfo.Length > MAX_ARCHIVE_SIZE_BYTES)
        {
            throw new InvalidDataException(
                $"Archive exceeds the maximum allowed size of {MAX_ARCHIVE_SIZE_BYTES} bytes.");
        }

        var outputPath = Path.ChangeExtension(fileInfo.FullName, ".json");
        var temporaryOutputPath = Path.Combine(
            fileInfo.DirectoryName ?? throw new InvalidOperationException("Archive directory could not be determined."),
            $".{Guid.NewGuid():N}.json.extracting");
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

        var jsonFileFound = false;

        try
        {
            _logger.LogInformation("Extracting archive: {ArchivePath}", archivePath);

            using (var fileStream = fileInfo.OpenRead())
            using (var zipStream = new ZipInputStream(fileStream))
            {
                zipStream.Password = archivePassword;
                ZipEntry entry;
                var entryCount = 0;
                long totalExpandedBytes = 0;
                var buffer = new byte[81920];

                while ((entry = zipStream.GetNextEntry()) != null)
                {
                    entryCount++;
                    if (entryCount > MAX_ARCHIVE_ENTRIES)
                    {
                        throw new InvalidDataException(
                            $"Archive contains more than the maximum allowed {MAX_ARCHIVE_ENTRIES} entries.");
                    }

                    if (entry.IsDirectory)
                        continue;

                    var isJson = !jsonFileFound
                        && !string.IsNullOrEmpty(entry.Name)
                        && entry.Name.EndsWith(".json", StringComparison.OrdinalIgnoreCase);
                    long jsonBytesWritten = 0;

                    await using var fileWriter = isJson
                        ? new FileStream(
                            temporaryOutputPath,
                            FileMode.CreateNew,
                            FileAccess.Write,
                            FileShare.None,
                            81920,
                            useAsync: true)
                        : null;

                    int bytesRead;
                    while ((bytesRead = await zipStream.ReadAsync(buffer)) > 0)
                    {
                        totalExpandedBytes += bytesRead;
                        if (totalExpandedBytes > MAX_EXPANDED_SIZE_BYTES)
                        {
                            throw new InvalidDataException(
                                $"Archive expands beyond the maximum allowed size of {MAX_EXPANDED_SIZE_BYTES} bytes.");
                        }

                        if (fileWriter is not null)
                        {
                            jsonBytesWritten += bytesRead;
                            if (jsonBytesWritten > MAX_JSON_SIZE_BYTES)
                            {
                                throw new InvalidDataException(
                                    $"JSON entry exceeds the maximum allowed size of {MAX_JSON_SIZE_BYTES} bytes.");
                            }

                            await fileWriter.WriteAsync(buffer.AsMemory(0, bytesRead));
                        }
                    }

                    jsonFileFound |= isJson;
                }
            }

            if (!jsonFileFound)
            {
                _logger.LogError("No JSON file found in archive: {ArchivePath}", archivePath);
                throw new InvalidOperationException($"No JSON file found in archive: {archivePath}");
            }

            File.Move(temporaryOutputPath, outputPath, overwrite: true);
            _logger.LogInformation("Successfully extracted JSON file: {OutputPath}", outputPath);
            return outputPath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting archive: {ArchivePath}", archivePath);

            // Clean up partial file if extraction failed
            try
            {
                if (File.Exists(temporaryOutputPath))
                    File.Delete(temporaryOutputPath);
            }
            catch (Exception cleanupEx)
            {
                _logger.LogWarning(cleanupEx, "Failed to delete partial output file: {OutputPath}", outputPath);
            }

            throw;
        }
    }
}
