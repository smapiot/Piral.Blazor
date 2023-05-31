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

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _cliProcess = await StartPiralCli();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _cliProcess?.Kill(true);
            return Task.CompletedTask;
        }

        private Task<Process> StartPiralCli()
        {
            var tcs = new TaskCompletionSource<Process>();
            var ct = new CancellationTokenSource(60 * 1000);
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
            
            var handler = new DataReceivedEventHandler((sender, e) =>
            {
                Console.WriteLine("[piral-cli] {0}", e.Data);

                if (e.Data?.IndexOf("Ready!") != -1)
                {
                    tcs.TrySetResult(process);
                }
            });

            process.ErrorDataReceived += handler;
            process.OutputDataReceived += handler;

            ct.Token.Register(() => tcs.TrySetCanceled(), useSynchronizationContext: false);

            process.Start();
            process.BeginErrorReadLine();
            process.BeginOutputReadLine();
            return tcs.Task;
        }
    }
}
