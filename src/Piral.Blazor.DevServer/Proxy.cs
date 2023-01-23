namespace Piral.Blazor.DevServer
{
    public static class Proxy
    {
        private const int _4kB = 4 * 1024;

        public static HttpRequestMessage CreateProxyHttpRequest(this HttpContext context, Uri uri)
        {
            var request = context.Request;
            var requestMessage = new HttpRequestMessage();
            var requestMethod = request.Method;

            if (!HttpMethods.IsGet(requestMethod) &&
                !HttpMethods.IsHead(requestMethod) &&
                !HttpMethods.IsDelete(requestMethod) &&
                !HttpMethods.IsTrace(requestMethod))
            {
                var streamContent = new StreamContent(request.Body);
                requestMessage.Content = streamContent;
            }

            // Copy the request headers
            foreach (var header in request.Headers)
            {
                if (!requestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray()) && requestMessage.Content is not null)
                {
                    requestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
                }
            }

            requestMessage.Headers.Host = uri.Authority;
            requestMessage.RequestUri = uri;
            requestMessage.Method = new HttpMethod(request.Method);
            return requestMessage;
        }

        public static async Task CopyProxyHttpResponse(this HttpContext context, HttpResponseMessage responseMessage)
        {
            var message = responseMessage ?? throw new ArgumentNullException(nameof(responseMessage));
            var response = context.Response;

            response.StatusCode = (int)message.StatusCode;

            foreach (var header in message.Headers)
            {
                response.Headers[header.Key] = header.Value.ToArray();
            }

            foreach (var header in message.Content.Headers)
            {
                response.Headers[header.Key] = header.Value.ToArray();
            }

            // SendAsync removes chunking from the response. This removes the header so it doesn't expect a chunked response.
            response.Headers.Remove("transfer-encoding");

            using var responseStream = await message.Content.ReadAsStreamAsync();
            await responseStream.CopyToAsync(response.Body, _4kB, context.RequestAborted);
        }
    }
}
