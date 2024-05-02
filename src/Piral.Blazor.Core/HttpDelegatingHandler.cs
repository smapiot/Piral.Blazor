using Piral.Blazor.Utils;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Piral.Blazor.Core;

public class HttpDelegatingHandler : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var interceptors = JSBridge.GetServices<IHttpInterceptor>();

        foreach (var interceptor in interceptors)
        {
            request = await interceptor.OnRequest(request, cancellationToken);
        }

        var response = await base.SendAsync(request, cancellationToken);

        foreach (var interceptor in interceptors)
        {
            response = await interceptor.OnResponse(response, cancellationToken);
        }

        return response;
    }
}
