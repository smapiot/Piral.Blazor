using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Microsoft.AspNetCore.Components.WebAssembly.DevServer.Server
{
    internal class App
    {
        /// <summary>
        /// Intended for framework test use only.
        /// </summary>
        public static IHost BuildWebHost(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureHostConfiguration(config =>
                {
                    var applicationPath = args.SkipWhile(a => a != "--applicationpath").Skip(1).FirstOrDefault();
                    var applicationDirectory = Path.GetDirectoryName(applicationPath);
                    var name = Path.ChangeExtension(applicationPath, ".StaticWebAssets.xml");

                    var inMemoryConfiguration = new Dictionary<string, string>
                    {
                        [WebHostDefaults.EnvironmentKey] = "Development",
                        ["Logging:LogLevel:Microsoft"] = "Warning",
                        ["Logging:LogLevel:Microsoft.Hosting.Lifetime"] = "Information",
                        [WebHostDefaults.StaticWebAssetsKey] = name,
                    };

                    config.AddInMemoryCollection(inMemoryConfiguration);
                    config.AddJsonFile(Path.Combine(applicationDirectory, "blazor-devserversettings.json"), optional: true, reloadOnChange: true);
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStaticWebAssets();
                    webBuilder.UseStartup<Startup>();
                }).Build();
    }
}