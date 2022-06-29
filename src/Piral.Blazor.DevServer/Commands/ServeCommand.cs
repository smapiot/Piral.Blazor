using Microsoft.AspNetCore.Components.WebAssembly.DevServer.Server;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Hosting;

namespace Microsoft.AspNetCore.Components.Web.DevServer.Commands
{
    internal class ServeCommand : CommandLineApplication
    {
        public ServeCommand(CommandLineApplication parent)
            // We pass arbitrary arguments through to the ASP.NET Core configuration
            : base(throwOnUnexpectedArg: false)
        {
            Parent = parent;

            Name = "serve";
            Description = "Serve requests to a Blazor application";

            HelpOption("-?|-h|--help");

            OnExecute(Execute);
        }

        private int Execute()
        {
            App.BuildWebHost(RemainingArguments.ToArray()).Run();
            return 0;
        }
    }
}