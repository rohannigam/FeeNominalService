using System.Text;
using Microsoft.IO;
using Serilog;
using Microsoft.AspNetCore.Http;
using System.Text.Json;

namespace FeeNominalService.Middleware;

public class RequestResponseLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly RecyclableMemoryStreamManager _streamManager;
    private readonly ILogger<RequestResponseLoggingMiddleware> _logger;

    public RequestResponseLoggingMiddleware(RequestDelegate next, ILogger<RequestResponseLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
        _streamManager = new RecyclableMemoryStreamManager();
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Log request
        var request = await FormatRequest(context.Request);
        var originalBodyStream = context.Response.Body;

        using var responseBody = _streamManager.GetStream();
        context.Response.Body = responseBody;

        try
        {
            await _next(context);
        }
        finally
        {
            // Log response
            var response = await FormatResponse(context.Response);
            var headers = FormatHeaders(context.Request.Headers, context.Response.Headers);

            _logger.LogInformation(
                "HTTP {RequestMethod} {RequestPath} completed with {StatusCode}. Request: {Request}. Response: {Response}. Headers: {Headers}",
                context.Request.Method,
                context.Request.Path,
                context.Response.StatusCode,
                request,
                response,
                headers);

            await responseBody.CopyToAsync(originalBodyStream);
        }
    }

    private async Task<string> FormatRequest(HttpRequest request)
    {
        request.EnableBuffering();

        var body = string.Empty;
        if (request.Body.Length > 0)
        {
            request.Body.Position = 0;
            body = await new StreamReader(request.Body, leaveOpen: true).ReadToEndAsync();
            request.Body.Position = 0;
        }

        // Try to format JSON if the content type is JSON
        if (request.ContentType?.Contains("application/json") == true && !string.IsNullOrEmpty(body))
        {
            try
            {
                var jsonDoc = JsonDocument.Parse(body);
                body = JsonSerializer.Serialize(jsonDoc, new JsonSerializerOptions 
                { 
                    WriteIndented = true 
                });
            }
            catch
            {
                // If JSON parsing fails, use the raw body
            }
        }

        return $"{request.Method} {request.Scheme}://{request.Host}{request.Path}{request.QueryString}\nBody: {body}";
    }

    private async Task<string> FormatResponse(HttpResponse response)
    {
        response.Body.Seek(0, SeekOrigin.Begin);
        var text = await new StreamReader(response.Body, leaveOpen: true).ReadToEndAsync();
        response.Body.Seek(0, SeekOrigin.Begin);

        // Try to format JSON if the content type is JSON
        if (response.ContentType?.Contains("application/json") == true && !string.IsNullOrEmpty(text))
        {
            try
            {
                var jsonDoc = JsonDocument.Parse(text);
                text = JsonSerializer.Serialize(jsonDoc, new JsonSerializerOptions 
                { 
                    WriteIndented = true 
                });
            }
            catch
            {
                // If JSON parsing fails, use the raw text
            }
        }

        return $"Status: {response.StatusCode}\nBody: {text}";
    }

    private string FormatHeaders(IHeaderDictionary requestHeaders, IHeaderDictionary responseHeaders)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("Request Headers:");
        foreach (var header in requestHeaders)
        {
            sb.AppendLine($"{header.Key}: {header.Value}");
        }

        sb.AppendLine("\nResponse Headers:");
        foreach (var header in responseHeaders)
        {
            sb.AppendLine($"{header.Key}: {header.Value}");
        }

        return sb.ToString();
    }
} 