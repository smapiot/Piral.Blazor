using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Piral.Blazor.Core;
using Piral.Blazor.Core.Dependencies;
using Xunit;

namespace Piral.Blazor.Tests
{
    public class PiralServiceProviderTests
    {
        [Fact]
        public void Pilet_Services_Should_Resolve_Global_Dependencies()
        {
            // global registrations
            var globalServices = new ServiceCollection();
            globalServices.AddSingleton<GlobalDependency>();
            var piralServiceProvider = new PiralServiceProvider(globalServices);
            var globalDependency = piralServiceProvider.GetRequiredService<GlobalDependency>();

            // moduleA registrations
            var moduleAServices = new ServiceCollection();
            moduleAServices.AddTransient<ModuleADependency>();
            var moduleAServiceProvider = piralServiceProvider.CreatePiletServiceProvider(moduleAServices);
            var moduleADependency = moduleAServiceProvider.GetRequiredService<ModuleADependency>();

            globalDependency.Should().Be(moduleADependency.Dependency);

            // moduleB registrations
            var moduleBServices = new ServiceCollection();
            moduleBServices.AddTransient<ModuleBDependency>();
            var moduleBServiceProvider = piralServiceProvider.CreatePiletServiceProvider(moduleBServices);
            var moduleBDependency = moduleBServiceProvider.GetRequiredService<ModuleBDependency>();

            globalDependency.Should().Be(moduleBDependency.Dependency);
        }

        // [Fact]
        // public void Pilet_Services_Should_Resolve_Global_Dependency_From_Other_Pilet()
        // {
        //     // global registrations
        //     var globalServices = new ServiceCollection();
        //     var piralServiceProvider = new PiralServiceProvider(globalServices);

        //     // moduleA registrations
        //     var moduleAGlobalServices = new ServiceCollection();
        //     moduleAGlobalServices.AddSingleton<ModuleAGlobalDependency>();
        //     piralServiceProvider.AddGlobalServices(moduleAGlobalServices);

        //     var moduleAServices = new ServiceCollection();
        //     var moduleAServiceProvider = piralServiceProvider.CreatePiletServiceProvider(moduleAServices);

        //     // moduleC registrations
        //     var moduleCServices = new ServiceCollection();
        //     moduleCServices.AddTransient<ModuleCDependency>();
        //     var moduleCServiceProvider = piralServiceProvider.CreatePiletServiceProvider(moduleCServices);

        //     // resolve dependencies
        //     var moduleAGlobalDependency = moduleAServiceProvider.GetRequiredService<ModuleAGlobalDependency>();
        //     var moduleCDependency = moduleCServiceProvider.GetRequiredService<ModuleCDependency>();

        //     // assert
        //     moduleAGlobalDependency.Should().Be(moduleCDependency.Dependency);
        // }

        // [Fact]
        // public void Declaring_Global_Dependencies_In_Two_Pilets_Should_Work()
        // {
        //     // global registrations from pilet 1
        //     var globalServices = new ServiceCollection();
        //     globalServices.AddSingleton<GlobalDependency>();
        //     var piralServiceProvider = new PiralServiceProvider(globalServices);

        //     // moduleA registrations
        //     var moduleAGlobalServices = new ServiceCollection();
        //     moduleAGlobalServices.AddSingleton<ModuleAGlobalDependency>();
        //     piralServiceProvider.AddGlobalServices(moduleAGlobalServices);

        //     var moduleAServices = new ServiceCollection();
        //     var moduleAServiceProvider = piralServiceProvider.CreatePiletServiceProvider(moduleAServices);

        //     // resolve dependencies
        //     var globDep = moduleAServiceProvider.GetRequiredService<GlobalDependency>();

        //     // moduleB registrations
        //     var moduleBGlobalServices = new ServiceCollection();
        //     moduleBGlobalServices.AddTransient<ModuleBDependency>();
        //     piralServiceProvider.AddGlobalServices(moduleBGlobalServices);

        //     var moduleBServices = new ServiceCollection();
        //     var moduleBServiceProvider = piralServiceProvider.CreatePiletServiceProvider(moduleBServices);

        //     // resolve dependencies
        //     var moduleDependency = moduleBServiceProvider.GetRequiredService<ModuleBDependency>();

        //     // assert
        //     globDep.Should().Be(moduleDependency.Dependency);
        // }

        // [Fact]
        // public void Pilet_Services_Registration_Should_Be_Commutative()
        // {
        //     // global registrations
        //     var globalServices = new ServiceCollection();
        //     var prialServiceProvider = new PiralServiceProvider(globalServices);

        //     // moduleC registrations
        //     var moduleCServices = new ServiceCollection();
        //     moduleCServices.AddTransient<ModuleCDependency>();
        //     var moduleCServiceProvider = prialServiceProvider.CreatePiletServiceProvider(moduleCServices);

        //     // moduleA registrations
        //     var moduleAGlobalServices = new ServiceCollection();
        //     moduleAGlobalServices.AddSingleton<ModuleAGlobalDependency>();
        //     prialServiceProvider.AddGlobalServices(moduleAGlobalServices);

        //     var moduleAServices = new ServiceCollection();
        //     var moduleAServiceProvider = prialServiceProvider.CreatePiletServiceProvider(moduleAServices);

        //     // resolve dependencies
        //     var moduleAGlobalDependency = moduleAServiceProvider.GetRequiredService<ModuleAGlobalDependency>();
        //     var moduleCDependency = moduleCServiceProvider.GetRequiredService<ModuleCDependency>();

        //     // assert
        //     moduleAGlobalDependency.Should().Be(moduleCDependency.Dependency);
        // }

        #region fakes

        class GlobalDependency { }

        class ModuleAGlobalDependency { }

        class ModuleADependency
        {
            public ModuleADependency(GlobalDependency dependency)
            {
                Dependency = dependency;
            }

            public GlobalDependency Dependency { get; }
        }

        class ModuleBDependency
        {
            public ModuleBDependency(GlobalDependency dependency)
            {
                Dependency = dependency;
            }

            public GlobalDependency Dependency { get; }
        }

        class ModuleCDependency
        {
            public ModuleCDependency(ModuleAGlobalDependency dependency)
            {
                Dependency = dependency;
            }

            public ModuleAGlobalDependency Dependency { get; }
        }

        #endregion
    }
}