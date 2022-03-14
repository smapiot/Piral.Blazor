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

            var moduleConatinerService = sp.GetRequiredService<IModuleContainerService>();

            // Act
            var spA = moduleConatinerService.Configure(typeof(PiletA.Module).Assembly);
            var spB = moduleConatinerService.Configure(typeof(PiletB.Module).Assembly);

            // Assert
            var dependencyA = spA.GetRequiredService<PiletA.DependencyA>();
            var dependencyB = spB.GetRequiredService<PiletB.DependencyB>();

            dependencyB.Should().Be(dependencyA.Dependency);
        }
    }
}
