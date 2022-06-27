using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DesignSystem
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddConsoleMessageService(this IServiceCollection services)
        {
            services.TryAddTransient<IConsoleMessageService, ConsoleMessageService>();
            return services;
        }

        public static IServiceCollection AddComponentLibServices(this IServiceCollection services)
        {
            return services
                .AddConsoleMessageService();
        }
    }
}
