using Microsoft.Extensions.DependencyInjection;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Piral.Blazor.Utils;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAccessCodeInterceptor(this IServiceCollection services)
    {
        return services.AddSingleton<IHttpInterceptor, AccessCodeHttpInterceptor>();
    }

    class AccessCodeHttpInterceptor(IPiletService piletService) : IHttpInterceptor
    {
        private readonly IPiletService _piletService = piletService;

        public async Task<HttpRequestMessage> OnRequest(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var token = await _piletService.GetAccessToken().ConfigureAwait(false);
            request.Headers.Add("Authorization", $"Bearer {token}");
            return request;
        }

        public Task<HttpResponseMessage> OnResponse(HttpResponseMessage response, CancellationToken cancellationToken) => Task.FromResult(response);
    }
}
