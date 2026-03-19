using PhoneMirror.Core.Execution;
using PhoneMirror.Core.Platform;
using PhoneMirror.Core.Services;

namespace PhoneMirror.Mcp;

class Program
{
    static async Task<int> Main(string[] args)
    {
        // Redirect stderr for diagnostic logging (stdout is reserved for MCP JSON-RPC)
        var logWriter = Console.Error;

        logWriter.WriteLine("adb-bridge MCP server starting...");

        try
        {
            // Wire up core services (same as the UI app, but without Avalonia/DI container)
            var platformService = new PlatformService();
            var processRunner = new ProcessRunner(platformService);
            var adbService = new AdbService(processRunner, platformService);
            var screenshotService = new ScreenshotService(adbService);

            // Verify ADB is available
            var adbAvailable = await adbService.IsAvailableAsync();
            if (adbAvailable)
            {
                logWriter.WriteLine($"ADB found at: {adbService.AdbPath}");
                await adbService.EnsureServerRunningAsync();
            }
            else
            {
                logWriter.WriteLine("WARNING: ADB not found. Tools will fail until ADB is available.");
                logWriter.WriteLine("Set ANDROID_HOME or add ADB to PATH.");
            }

            var toolHandler = new AdbToolHandler(adbService, screenshotService, processRunner);
            var server = new McpServer(toolHandler);

            using var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (_, e) =>
            {
                e.Cancel = true;
                cts.Cancel();
            };

            logWriter.WriteLine("MCP server ready. Listening on stdin...");
            await server.RunAsync(cts.Token);

            logWriter.WriteLine("MCP server shutting down.");
            return 0;
        }
        catch (Exception ex)
        {
            logWriter.WriteLine($"Fatal error: {ex}");
            return 1;
        }
    }
}
