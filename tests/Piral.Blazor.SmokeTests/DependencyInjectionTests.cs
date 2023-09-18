using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Piral.Blazor.Core;
using Piral.Blazor.Core.Dependencies;
using Xunit;

namespace Piral.Blazor.SmokeTests
{
    public class DependencyInjectionTests
    {
        [Fact]
        public void PiletA_Should_Resolve_Global_Dependency_Registerd_By_PiletB()
        {
            // Arrange
            var services = new ServiceCollection();

            services.AddLogging();
            services.AddSingleton<IModuleContainerService, ModuleContainerService>();

            var sp = new PiralServiceProvider(services);

            var moduleContainerService = sp.GetRequiredService<IModuleContainerService>();
            var piletService = new PiletService(null, null, "http://localhost:1234/$pilet-api/0/pilet.dll");

            var asc1 = new AssemblyLoadContext("pilet-a");
            var asc2 = AssemblyLoadContext.Default;
            var ass1 = asc1.LoadFromStream();
            var ass2 = asc2.LoadFromStream();

            var DependencyA = ass1.FindType("DependencyA");
            var DependencyB = ass2.FindType("DependencyB");

            // Act
            moduleContainerService.ConfigureModule(ass2, piletService);
            var spB = moduleContainerService.GetProvider(ass2);

            moduleContainerService.ConfigureModule(ass1, piletService);
            var spA = moduleContainerService.GetProvider(ass1);

            // Assert
            var dependencyA = spA.GetRequiredService(DependencyA);
            var dependencyB = spB.GetRequiredService(DependencyB);

            dependencyB.Should().Be(dependencyA.Dependency);
        }
    }
}
