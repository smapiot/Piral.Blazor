using Piral.Blazor.Utils;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Piral.Blazor.Core;

public class HttpDelegatingHandler : DelegatingHandler
{
    public HttpDelegatingHandler()
        : base(new HttpClientHandler())
    {
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var interceptors = JSBridge.GetServices<IHttpInterceptor>();

        foreach (var interceptor in interceptors)
        {
            try
            {
                request = await interceptor.OnRequest(request, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.WriteLine("The request interceptor has thrown an exception: {0}", ex.Message);
            }
        }

        var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

        foreach (var interceptor in interceptors)
        {
            try
            {
                response = await interceptor.OnResponse(response, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.WriteLine("The response interceptor has thrown an exception: {0}", ex.Message);
            }
        }

        return response;
    }
}
