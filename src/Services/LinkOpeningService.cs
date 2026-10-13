using System.Diagnostics;
using Microsoft.Extensions.Logging;
using PCRadio.Services.Interfaces;

namespace PCRadio.Services;

public class LinkOpeningService : ILinkOpeningService
{
    private readonly ILogger<LinkOpeningService> _logger;

    public LinkOpeningService(ILogger<LinkOpeningService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void OpenUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return;

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            return;
        }

        try
        {
            using var process = Process.Start(new ProcessStartInfo(uri.AbsoluteUri)
            {
                UseShellExecute = true
            });

            if (process is null)
            {
                throw new InvalidOperationException($"Failed to open URL '{uri}'.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error opening URL: {Url}", uri);
        }
    }
}
