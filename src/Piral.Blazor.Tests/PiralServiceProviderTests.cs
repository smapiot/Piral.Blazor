using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Piral.Blazor.Core;
using Xunit;

namespace Piral.Blazor.Tests
{
    public class PiralServiceProviderTests
    {
        [Fact]
        public void Pilet_Service_Should_Resolve_Global_Service()
        {
            // global registrations
            var globalServices = new ServiceCollection();
            globalServices.AddSingleton<GlobalDependency>();
            var serviceProvider = new PiralServiceProvider(globalServices);
            var globalDependency = serviceProvider.GetRequiredService<GlobalDependency>();

            // pilet registrations
            var piletServices = new ServiceCollection();
            piletServices.AddTransient<PiletDependency>();
            serviceProvider.AddPiletServices(piletServices);
            var piletDependency = serviceProvider.GetRequiredService<PiletDependency>();

            globalDependency.Should().Be(piletDependency.Dependency);
        }

        #region fakes

        class GlobalDependency { }

        class PiletDependency
        {
            public PiletDependency(GlobalDependency dependency)
            {
                Dependency = dependency;
            }

            public GlobalDependency Dependency { get; }
        }

        class Generic { }

        #endregion
    }
}