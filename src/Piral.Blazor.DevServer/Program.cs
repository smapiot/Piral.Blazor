using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using Piral.Blazor.DevServer;
using System.Diagnostics;
using System.Net;
using System.Net.Mime;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

var wwwRoot = "wwwroot";
var piletApiSegment = "/$pilet-api";
var cliPort = GetFreeTcpPort();
var feedHost = $"localhost:{cliPort}";
var feedUrl = $"http://{feedHost}";
var applicationPath = args.SkipWhile(a => a != "--applicationpath").Skip(1).First();
var outPath = args.SkipWhile(a => a != "--outdir").Skip(1).First();
var applicationDirectory = Path.GetDirectoryName(applicationPath)!;
var swaPath = Path.ChangeExtension(applicationPath, ".staticwebassets.runtime.json");
var appId = Path.GetFileNameWithoutExtension(applicationPath);
var staticAssets = !File.Exists(swaPath) ? Path.ChangeExtension(applicationPath, ".StaticWebAssets.xml") : swaPath;
var piletDir = Path.Combine(Environment.CurrentDirectory, outPath, appId);
var piletJsonPath = Path.Combine(piletDir, "pilet.json");
var packageJsonPath = Path.Combine(piletDir, "package.json");
var piralInstance = FindPiralInstance(piletJsonPath, packageJsonPath);
var distDir = Path.Combine(piletDir, "dist");
var www = Path.Combine(piletDir, "node_modules", piralInstance, "app");
var wwwProvider = new PhysicalFileProvider(www);
var contentTypeProvider = CreateStaticFileTypeProvider();
var cliProcess = StartPiralCli(piletDir, cliPort);

Console.WriteLine("Starting Piral.Blazor.DevServer ...");
Console.WriteLine("");
Console.WriteLine("  applicationPath = {0}", applicationPath);
Console.WriteLine("  piralInstance = {0}", piralInstance);
Console.WriteLine("  piletDir = {0}", piletDir);
Console.WriteLine("  outPath = {0}", outPath);
Console.WriteLine("  appId = {0}", appId);
Console.WriteLine("  feed = {0}", feedUrl);
Console.WriteLine("");

static string FindPiralInstance(string piletJsonPath, string packageJsonPath)
{
    if (File.Exists(piletJsonPath))
    {
        using var jsonStream = File.Open(piletJsonPath, FileMode.Open);
        var pilet = JsonSerializer.Deserialize<PiletJson>(jsonStream);
        var key = pilet?.PiralInstances?.Keys.FirstOrDefault();

        if (key is not null)
        {
            return key;
        }
    }

    if (File.Exists(packageJsonPath))
    {
        using var jsonStream = File.Open(packageJsonPath, FileMode.Open);
        var pilet = JsonSerializer.Deserialize<PackageJson>(jsonStream);
        var key = pilet?.Piral?.Name;

        if (key is not null)
        {
            return key;
        }
    }

    throw new InvalidOperationException("No Piral instance has been found. Cannot start the server.");
}

static int GetFreeTcpPort()
{
    var l = new TcpListener(IPAddress.Loopback, 0);
    l.Start();
    var port = ((IPEndPoint)l.LocalEndpoint).Port;
    l.Stop();
    return port;
}

static Process StartPiralCli(string piletDir, int cliPort)
{
    var isWindows = Environment.OSVersion.Platform == PlatformID.Win32NT;
    var npx = isWindows ? "cmd.exe" : "npx";
    var npxPrefix = isWindows ? "/c npx.cmd " : "";

    var process = Process.Start(new ProcessStartInfo
    {
        FileName = npx,
        WorkingDirectory = piletDir,
        UseShellExecute = false,
        CreateNoWindow = true,
        Arguments = $"{npxPrefix}pilet debug --port {cliPort}",
        RedirectStandardOutput = true,
        RedirectStandardError = true,
    })!;

    process.ErrorDataReceived += (sender, e) => Console.WriteLine("[piral-cli] {0}", e.Data);
    process.OutputDataReceived += (sender, e) => Console.WriteLine("[piral-cli] {0}", e.Data);
    AppDomain.CurrentDomain.DomainUnload += (sender, e) => process.Kill();
    AppDomain.CurrentDomain.ProcessExit += (sender, e) => process.Kill();

    process.Start();
    process.BeginErrorReadLine();
    process.BeginOutputReadLine();
    return process;
}

static void AppendHeaders(HttpContext context, WebApplication app)
{
    var headers = context.Response.Headers;
    headers.Append("Blazor-Environment", app.Environment.EnvironmentName);
    headers.Append("Cache-Control", "no-cache");

    if (app.Environment.IsDevelopment())
    {
        if (Environment.GetEnvironmentVariable("DOTNET_MODIFIABLE_ASSEMBLIES") is string dotnetModifiableAssemblies)
        {
            headers.Append("DOTNET-MODIFIABLE-ASSEMBLIES", dotnetModifiableAssemblies);
        }

        if (Environment.GetEnvironmentVariable("__ASPNETCORE_BROWSER_TOOLS") is string blazorWasmHotReload)
        {
            headers.Append("ASPNETCORE-BROWSER-TOOLS", "true");
        }
    }
}

static void AppendContentType(HttpContext context, IContentTypeProvider contentTypeProvider, string filePath)
{
    contentTypeProvider.TryGetContentType(filePath, out var contentType);
    context.Response.ContentType = contentType ?? "application/octet-stream";
}

static IContentTypeProvider CreateStaticFileTypeProvider()
{
    var contentTypeProvider = new FileExtensionContentTypeProvider();
    contentTypeProvider.Mappings.TryAdd(".dll", MediaTypeNames.Application.Octet);
    contentTypeProvider.Mappings.TryAdd(".pdb", MediaTypeNames.Application.Octet);
    contentTypeProvider.Mappings.TryAdd(".br", MediaTypeNames.Application.Octet);
    contentTypeProvider.Mappings.TryAdd(".dat", MediaTypeNames.Application.Octet);
    contentTypeProvider.Mappings.TryAdd(".blat", MediaTypeNames.Application.Octet);
    contentTypeProvider.Mappings.TryAdd(".wasm", "application/wasm");
    return contentTypeProvider;
}

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    ContentRootPath = applicationDirectory,
    WebRootPath = wwwRoot,
});

var inMemoryConfiguration = new Dictionary<string, string>
{
    [WebHostDefaults.EnvironmentKey] = "Development",
    ["Logging:LogLevel:Microsoft"] = "Warning",
    ["Logging:LogLevel:Microsoft.Hosting.Lifetime"] = "Information",
    [WebHostDefaults.StaticWebAssetsKey] = staticAssets,
};

builder.Configuration.AddInMemoryCollection(inMemoryConfiguration!);
builder.Configuration.AddJsonFile(Path.Combine(applicationDirectory, "blazor-devserversettings.json"), optional: true, reloadOnChange: true);
builder.Services.AddHttpClient();

var app = builder.Build();
var forwardedPaths = app.Configuration.GetValue("forwardedPaths", new List<string> { });

app.UseDeveloperExceptionPage();
app.UseWebSockets();
app.UseWebAssemblyDebugging();
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = (res) => AppendHeaders(res.Context, app),
    FileProvider = wwwProvider,
    ServeUnknownFileTypes = true,
    ContentTypeProvider = contentTypeProvider,
});
app.Use(async (context, next) =>
{
    var reqPath = context.Request.Path.Value!;
    var host = context.Request.Host;
    var scheme = context.Request.IsHttps ? "https" : "http";

    // right now we support a single-pilet only; in the future multiple pilets may
    // be debugged, too
    if (reqPath.StartsWith($"{piletApiSegment}/0"))
    {
        var path = reqPath.Replace($"{piletApiSegment}/0/", "");
        var filePath = Path.Combine(distDir, path);
        AppendHeaders(context, app);
        AppendContentType(context, contentTypeProvider, path);
        await context.Response.SendFileAsync(filePath);
    }
    else if (reqPath.StartsWith(piletApiSegment) && context.WebSockets.IsWebSocketRequest)
    {
        using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
        using var client = new ClientWebSocket();
        var target = new Uri($"ws://{feedHost}{piletApiSegment}");
        await client.ConnectAsync(target, CancellationToken.None);

        while (true)
        {
            var result = new WebSocketReceiveResult(0, WebSocketMessageType.Text, false);
            var buffer = new ArraySegment<byte>(new byte[2048]);
            using var ms = new MemoryStream();

            while (!result.EndOfMessage)
            {
                result = await client.ReceiveAsync(buffer, CancellationToken.None);
                ms.Write(buffer.Array!, buffer.Offset, result.Count);
            }

            if (result.MessageType == WebSocketMessageType.Close)
            {
                break;
            }

            ms.Seek(0, SeekOrigin.Begin);

            using var reader = new StreamReader(ms, Encoding.UTF8);
            var json = await reader.ReadToEndAsync();
            var newJson = json.Replace(feedUrl, $"{scheme}://{host}");

            if (webSocket.State != WebSocketState.Open)
            {
                break;
            }

            await webSocket.SendAsync(Encoding.UTF8.GetBytes(newJson), WebSocketMessageType.Text, true, CancellationToken.None);
        }

        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, null, CancellationToken.None);
    }
    else if (reqPath.StartsWith(piletApiSegment) && context.Request.Method == "GET")
    {
        var httpFactory = context.RequestServices.GetService<IHttpClientFactory>()!;
        var client = httpFactory.CreateClient();
        var url = $"{feedUrl}{reqPath}";
        var json = await client.GetStringAsync(url);
        var newJson = json.Replace(feedUrl, $"{scheme}://{host}");
        AppendContentType(context, contentTypeProvider, "meta.json");
        await context.Response.WriteAsync(newJson);
    }
    else if (forwardedPaths.Any(path => reqPath.StartsWith(path)))
    {
        var httpFactory = context.RequestServices.GetService<IHttpClientFactory>()!;
        var client = httpFactory.CreateClient();
        var query = context.Request.QueryString.Value ?? string.Empty;
        var url = new Uri($"{feedUrl}{reqPath}{query}");
        var request = context.CreateProxyHttpRequest(url);
        var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, context.RequestAborted);
        await context.CopyProxyHttpResponse(response);
    }
    else
    {
        await next(context);
    }
});
app.UseRouting();
#pragma warning disable ASP0014 // Suggest using top level route registrations
app.UseEndpoints(endpoints =>
{
    endpoints.MapFallbackToFile("index.html", new StaticFileOptions
    {
        OnPrepareResponse = (res) => AppendHeaders(res.Context, app),
        FileProvider = wwwProvider,
    });
});
#pragma warning restore ASP0014 // Suggest using top level route registrations

app.Run();
