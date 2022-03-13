using Microsoft.Extensions.DependencyInjection;

namespace Piral.Blazor.SmokeTests.PiletB
{
    public class Module
    {
        public static void ConfigureShared(IServiceCollection services)
        {
            services.AddSingleton<DependencyB>();
        }
    }

    public class DependencyB { }
}
