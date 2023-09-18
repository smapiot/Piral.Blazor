using Microsoft.Extensions.DependencyInjection;
using Piral.Blazor.SmokeTests.PiletB;

namespace Piral.Blazor.SmokeTests.PiletA
{
    public class Module
    {
        public static void ConfigureServices(IServiceCollection services)
        {
            services.AddTransient<DependencyA>();
            services.AddTransient<DependencyB>();
        }
    }

    public class DependencyA
    {
        public DependencyA(DependencyB dependency)
        {
            Dependency = dependency;
        }

        public DependencyB Dependency { get; }
    }
}
