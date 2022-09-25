using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Piral.Blazor.Core;
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
            var piletService = new PiletService("http://localhost:1234/$pilet-api/0/pilet.dll");

            // Act
            var spA = moduleContainerService.ConfigureModule(typeof(PiletA.Module).Assembly, piletService);
            var spB = moduleContainerService.ConfigureModule(typeof(PiletB.Module).Assembly, piletService);

            // Assert
            var dependencyA = spA.GetRequiredService<PiletA.DependencyA>();
            var dependencyB = spB.GetRequiredService<PiletB.DependencyB>();

            dependencyB.Should().Be(dependencyA.Dependency);
        }
    }
}
