using System.Text;
using Microsoft.IO;
using Serilog;
using Microsoft.AspNetCore.Http;
using System.Text.Json;
using FeeNominalService.Utils;
using Microsoft.Extensions.Configuration;

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
            // Ensure the response is fully written
            await responseBody.FlushAsync();
            
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

            // Copy the response back to the original stream
            responseBody.Position = 0;
            await responseBody.CopyToAsync(originalBodyStream);
        }
    }

    private async Task<string> FormatRequest(HttpRequest request)
    {
        request.EnableBuffering();

        var body = string.Empty;
        
        // Always try to read the body, regardless of Length
        request.Body.Position = 0;
        body = await new StreamReader(request.Body, leaveOpen: true).ReadToEndAsync();
        request.Body.Position = 0;

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
                
                // Apply masking to sensitive data in request body
                body = MaskSensitiveData(body);
            }
            catch (JsonException)
            {
                // If JSON parsing fails, apply masking to raw body
                body = MaskSensitiveData(body);
            }
            catch (Exception)
            {
                // If any other error occurs, apply masking to raw body
                body = MaskSensitiveData(body);
            }
        }

        return $"{request.Method} {request.Scheme}://{request.Host}{request.Path}{request.QueryString}\nBody: {body}";
    }

    private async Task<string> FormatResponse(HttpResponse response)
    {
        try
        {
            // Ensure we're at the beginning of the stream
            response.Body.Position = 0;
            
            // Check if there's actually content in the stream
            if (response.Body.Length == 0)
            {
                return $"Status: {response.StatusCode}\nBody: [Empty response]";
            }

            var text = await new StreamReader(response.Body, leaveOpen: true).ReadToEndAsync();
            
            // Reset position for the next reader
            response.Body.Position = 0;

            // Try to format JSON if the content type is JSON
            if (response.ContentType?.Contains("application/json") == true && !string.IsNullOrEmpty(text))
            {
                try
                {
                    var jsonDoc = JsonDocument.Parse(text);
                    var formattedText = JsonSerializer.Serialize(jsonDoc, new JsonSerializerOptions 
                    { 
                        WriteIndented = true 
                    });
                    
                    // Mask sensitive data for API key endpoints
                    var maskedText = MaskSensitiveData(formattedText);
                    
                    return $"Status: {response.StatusCode}\nBody: {maskedText}";
                }
                catch (JsonException)
                {
                    // If JSON parsing fails, return the raw text with masking
                    var maskedText = MaskSensitiveData(text);
                    return $"Status: {response.StatusCode}\nBody: {maskedText}";
                }
                catch (Exception)
                {
                    // If any other error occurs, return the raw text with masking
                    var maskedText = MaskSensitiveData(text);
                    return $"Status: {response.StatusCode}\nBody: {maskedText}";
                }
            }
            else
            {
                return $"Status: {response.StatusCode}\nBody: {text}";
            }
        }
        catch (Exception ex)
        {
            return $"Status: {response.StatusCode}\nBody: [Error reading response body: {ex.Message}]";
        }
    }

    private string MaskSensitiveData(string jsonText)
    {
        if (string.IsNullOrEmpty(jsonText))
            return jsonText;

        var maskedText = jsonText;
        
        // Mask "secret" field values - show first 4 and last 4 characters
        maskedText = System.Text.RegularExpressions.Regex.Replace(
            maskedText, 
            @"""secret""\s*:\s*""([^""]+)""", 
            match =>
            {
                var value = match.Groups[1].Value;
                if (value.Length <= 8)
                {
                    return @"""secret"": ""****""";
                }
                return $@"""secret"": ""{value.Substring(0, 4)}...{value.Substring(value.Length - 4)}""";
            }
        );
        
        // Mask "apiKey" field values - show first 4 and last 4 characters
        maskedText = System.Text.RegularExpressions.Regex.Replace(
            maskedText, 
            @"""apiKey""\s*:\s*""([^""]+)""", 
            match =>
            {
                var value = match.Groups[1].Value;
                if (value.Length <= 8)
                {
                    return @"""apiKey"": ""****""";
                }
                return $@"""apiKey"": ""{value.Substring(0, 4)}...{value.Substring(value.Length - 4)}""";
            }
        );
        
        // Mask "jwt_token" field values in credentials - show first 4 and last 4 characters
        maskedText = System.Text.RegularExpressions.Regex.Replace(
            maskedText, 
            @"""jwt_token""\s*:\s*""([^""]+)""", 
            match =>
            {
                var value = match.Groups[1].Value;
                if (value.Length <= 8)
                {
                    return @"""jwt_token"": ""****""";
                }
                return $@"""jwt_token"": ""{value.Substring(0, 4)}...{value.Substring(value.Length - 4)}""";
            }
        );
        
        return maskedText;
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