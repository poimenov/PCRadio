using System.Diagnostics;
using ICSharpCode.SharpZipLib.Zip;
using Microsoft.Extensions.Logging;
using PCRadio.Services.Interfaces;

namespace PCRadio.Services;

public class ArchiveExtractService : IArchiveExtractService
{
    ILogger<ArchiveExtractService> _logger;
    public ArchiveExtractService(ILogger<ArchiveExtractService> logger)
    {
        _logger = logger;
    }

    public async Task<string> ExtractArchiveAsync(string archivePath, string archivePassword)
    {
        FileInfo fi = new FileInfo(archivePath);
        if (!fi.Exists)
        {
            _logger.LogError($"File: {archivePath} does not exist");
            throw new FileNotFoundException("Archive file not found.", archivePath);
        }

        var outputPath = fi.FullName.Replace(fi.Extension, ".json");

        await using (ZipInputStream s = new ZipInputStream(fi.OpenRead()))
        {
            s.Password = archivePassword;
            ZipEntry theEntry;
            while ((theEntry = s.GetNextEntry()) != null)
            {
                if (!string.IsNullOrEmpty(theEntry.Name) && theEntry.Name.EndsWith(".json"))
                {
                    await using (FileStream streamWriter = File.Create(outputPath))
                    {
                        int size = 2048;
                        byte[] data = new byte[2048];
                        while (true)
                        {
                            size = await s.ReadAsync(data, 0, data.Length);
                            if (size > 0)
                            {
                                await streamWriter.WriteAsync(data, 0, size);
                            }
                            else
                            {
                                break;
                            }
                        }
                    }
                }
            }
        }

        _logger.LogInformation($"Extracted successfully");
        Debug.WriteLine($"Extracted {archivePath} to {outputPath}");

        return outputPath;
    }
}
