using System.Diagnostics;
using Microsoft.Extensions.Logging;
using PCRadio.Services.Interfaces;

namespace PCRadio.Services;

public class ProcessService : IProcessService
{
    private readonly ILogger<IProcessService> _logger;
    public ProcessService(ILogger<IProcessService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    public void Run(string command, string arguments)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(command))
            {
                throw new ArgumentException("Command cannot be null or whitespace.", nameof(command));
            }

            using (var process = GetProcess(command, arguments))
            {
                if (!process.Start())
                    throw new InvalidOperationException($"Process '{command}' failed to start.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting process: {Command} {Arguments}", command, arguments);
        }
    }

    private static Process GetProcess(string command, string arguments)
    {
        var processStartInfo = new ProcessStartInfo(command)
        {
            RedirectStandardOutput = false,
            UseShellExecute = false,
            CreateNoWindow = false,
            Arguments = arguments
        };

        return new Process { StartInfo = processStartInfo };
    }
}
