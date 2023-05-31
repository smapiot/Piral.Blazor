using System.Diagnostics;

namespace Piral.Blazor.DevServer
{
    public class PiralCliService : IHostedService
    {
        private Process? _cliProcess;
        private string _piletDir;
        private int _cliPort;
        private string? _feed;

        public PiralCliService(string piletDir, int cliPort, IConfiguration? configuration)
        {
            _piletDir = piletDir;
            _cliPort = cliPort;
            _feed = configuration?.GetSection("Piral").Get<PiralOptions>()?.FeedUrl;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _cliProcess = StartPiralCli();
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _cliProcess?.Kill(true);
            return Task.CompletedTask;
        }

        private Process StartPiralCli()
        {
            var app = Process.GetCurrentProcess();
            var isWindows = Environment.OSVersion.Platform == PlatformID.Win32NT;
            var npx = isWindows ? "node.exe" : "node";
            var npxPrefix = "_debug.js";
            var extraArgs = !string.IsNullOrEmpty(_feed) ? $" --feed {_feed}" : "";
            var content = "const pid = +process.argv.pop();\r\n\r\nfunction pidIsRunning() {\r\n  try {\r\n    process.kill(pid, 0);\r\n    return true;\r\n  } catch (e) {\r\n    return false;\r\n  }\r\n}\r\n\r\nsetInterval(() => {\r\n  if (!pidIsRunning()) {\r\n    process.exit(0);\r\n  }\r\n}, 1000);\r\n\r\nrequire(\"piral-cli/lib/pilet-cli\");\r\n";

            File.WriteAllText(Path.Join(_piletDir, npxPrefix), content);

            var process = Process.Start(new ProcessStartInfo
            {
                FileName = npx,
                WorkingDirectory = _piletDir,
                UseShellExecute = false,
                CreateNoWindow = true,
                Arguments = $"{npxPrefix} debug --port {_cliPort}{extraArgs} {app.Id}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            })!;

            process.ErrorDataReceived += (sender, e) => Console.WriteLine("[piral-cli] {0}", e.Data);
            process.OutputDataReceived += (sender, e) => Console.WriteLine("[piral-cli] {0}", e.Data);

            process.Start();
            process.BeginErrorReadLine();
            process.BeginOutputReadLine();
            return process;
        }
    }
}
