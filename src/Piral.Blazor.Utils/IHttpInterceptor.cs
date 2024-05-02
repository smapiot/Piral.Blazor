using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Piral.Blazor.Utils;

public interface IHttpInterceptor
{
    Task<HttpRequestMessage> OnRequest(HttpRequestMessage request, CancellationToken cancellationToken);

    Task<HttpResponseMessage> OnResponse(HttpResponseMessage response, CancellationToken cancellationToken);
}
