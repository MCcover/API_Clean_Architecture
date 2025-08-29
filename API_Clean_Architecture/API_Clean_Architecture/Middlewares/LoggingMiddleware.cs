using System.Diagnostics;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using API.API_Clean_Architecture.Models.Logging;
using Serilog;

namespace API.API_Clean_Architecture.Middlewares;

public class LoggingMiddleware {
    private readonly RequestDelegate _next;

    private readonly string[] _sensitiveHeaders = ["Authorization", "Cookie", "Set-Cookie"];

    public LoggingMiddleware(RequestDelegate next) {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context) {
        var path = context.Request.Path.Value?.ToLowerInvariant() ?? "";
        
        if (path.StartsWith("/swagger") || path.StartsWith("/api/docs"))
        {
            await _next(context);
            return;
        }
        
        var traceId = Activity.Current?.Id ?? context.TraceIdentifier;
        var requestTimeUtc = DateTime.UtcNow;
        
        context.Request.EnableBuffering();
        var requestBody = await ReadStreamAsync(context.Request.Body);
        context.Request.Body.Position = 0;

        var requestHeaders = context.Request.Headers
            .Where(h => !_sensitiveHeaders.Contains(h.Key))
            .ToDictionary(h => h.Key, h => h.Value.ToString());

        var userClaims = context.User?.Claims?.Select(c => new KeyValuePair<string, string>(c.Type, c.Value)).ToList();
        
        var originalResponseBodyStream = context.Response.Body;
        using var responseBodyMs = new MemoryStream();
        context.Response.Body = responseBodyMs;

        var stopwatch = Stopwatch.StartNew();
        try {
            await _next(context);
        } catch (Exception ex) {
            // ignored
        }
        stopwatch.Stop();

        StreamReader? reader = null;
        GZipStream? decompressedStream = null;
        
        responseBodyMs.Seek(0, SeekOrigin.Begin);
        if (context.Response.Headers.TryGetValue("Content-Encoding", out var encoding) && 
            encoding.ToString().Equals("gzip", StringComparison.OrdinalIgnoreCase)){
            decompressedStream = new GZipStream(responseBodyMs, CompressionMode.Decompress, leaveOpen:true);
            reader = new StreamReader(decompressedStream);
        } else {
            reader = new StreamReader(responseBodyMs);
        }
        
        var responseBody = await reader.ReadToEndAsync();
        responseBodyMs.Seek(0, SeekOrigin.Begin);
        await responseBodyMs.CopyToAsync(originalResponseBodyStream);
        
        var responseHeaders = context.Response.Headers.Where(h => !_sensitiveHeaders.Contains(h.Key))
                                                      .ToDictionary(h => h.Key, h => h.Value.ToString());
        
        var logEntry = new HttpLogEntry {
            TraceId = traceId,
            Method = context.Request.Method,
            Scheme = context.Request.Scheme,
            Host = context.Request.Host.Value,
            Path = context.Request.Path,
            Query = context.Request.QueryString.ToString(),
            Headers = requestHeaders,
            Cookies = context.Request.Cookies.ToDictionary(c => c.Key, c => c.Value),
            Body = requestBody,
            ContentLength = context.Request.ContentLength,
            Protocol = context.Request.Protocol,
            ClientIP = context.Connection.RemoteIpAddress?.ToString() ?? string.Empty,
            RemotePort = context.Connection.RemotePort,
            User = context.User?.Identity?.Name ?? string.Empty,
            Claims = userClaims ?? [],
            RequestTimeUtc = requestTimeUtc,
            StatusCode = context.Response.StatusCode,
            ResponseHeaders = responseHeaders,
            ResponseBody = responseBody,
            ResponseContentLength = context.Response.ContentLength,
            ElapsedMilliseconds = stopwatch.ElapsedMilliseconds,
            ResponseTimeUtc = DateTime.UtcNow,
            MachineName = Environment.MachineName,
            EnvironmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? string.Empty
        };

        //TODO: Guardar en BD
        
        Log.Information("{@LogEntry}", logEntry);

        if (reader != null) {
            reader.Close();
            reader.Dispose();
        } 

        if (decompressedStream != null) {
            decompressedStream.Close();
            await decompressedStream.DisposeAsync();
        } 
        
    }

    private async Task<string> ReadStreamAsync(Stream stream) {
        stream.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);
        var body = await reader.ReadToEndAsync();
        stream.Seek(0, SeekOrigin.Begin);
        return body;
    }
    
}