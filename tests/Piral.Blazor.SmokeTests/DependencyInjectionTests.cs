using System.Linq;
using System.Runtime.Loader;
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
            var ass1 = asc1.LoadFromAssemblyPath(typeof(PiletA.DependencyA).Assembly.Location);
            var ass2 = asc2.LoadFromAssemblyPath(typeof(PiletB.DependencyB).Assembly.Location);

            var DependencyA = ass1.ExportedTypes.First(m => m.Name == "DependencyA");
            var DependencyB = ass2.ExportedTypes.First(m => m.Name == "DependencyB");

            // Act
            moduleContainerService.ConfigureModule(ass2, piletService);
            var spB = moduleContainerService.GetProvider(ass2);

            moduleContainerService.ConfigureModule(ass1, piletService);
            var spA = moduleContainerService.GetProvider(ass1);

            // Assert
            var dependencyA = spA.GetRequiredService(DependencyA);
            var dependencyB = spB.GetRequiredService(DependencyB);

            var depBofA = DependencyA.GetProperty("Dependency").GetValue(dependencyA);

            dependencyB.Should().NotBe(depBofA);
        }
    }
}
