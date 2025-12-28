using System.Text;
using BuildingBlocks.Extensions;
using Microsoft.AspNetCore.Http;
using Serilog;
using Serilog.Events;

namespace BuildingBlocks.SerilogLogging.Utils;

public static class SerilogRequestUtility
{
    public const string HttpMessageTemplate =
        "RequestIp:{RequestIp}  HTTP {RequestMethod} {RequestPath} QueryString:{QueryString} Body:{Body}  responded {StatusCode} in {Elapsed:0.0000} ms  LexiCraft";


    public static LogEventLevel GetRequestLevel(HttpContext ctx, double _, Exception? ex)
    {
        return ex is null && ctx.Response.StatusCode <= 499 ? IgnoreRequest(ctx) : LogEventLevel.Error;
    }


    private static LogEventLevel IgnoreRequest(HttpContext ctx)
    {
        var path = ctx.Request.Path.Value;

        return LogEventLevel.Information;
    }

    /// <summary>
    ///     从Request中增加附属属性
    /// </summary>
    /// <param name="diagnosticContext"></param>
    /// <param name="httpContext"></param>
    public static void EnrichFromRequest(IDiagnosticContext diagnosticContext, HttpContext httpContext)
    {
        var request = httpContext.Request;

        diagnosticContext.Set("RequestHost", request.Host);
        diagnosticContext.Set("RequestScheme", request.Scheme);
        diagnosticContext.Set("Protocol", request.Protocol);
        diagnosticContext.Set("RequestIp", httpContext.GetRequestIp());

        if (request.Method == HttpMethods.Get)
        {
            diagnosticContext.Set("QueryString",
                request.QueryString.HasValue ? request.QueryString.Value : string.Empty);
            diagnosticContext.Set("Body", string.Empty);
        }
        else
        {
            diagnosticContext.Set("QueryString",
                request.QueryString.HasValue ? request.QueryString.Value : string.Empty);
            diagnosticContext.Set("Body", request.ContentLength > 0 ? request.GetRequestBody() : string.Empty);
        }

        if (httpContext.Response.ContentType != null)
            diagnosticContext.Set("ContentType", httpContext.Response.ContentType);

        var endpoint = httpContext.GetEndpoint();
        if (endpoint?.DisplayName != null) diagnosticContext.Set("EndpointName", endpoint.DisplayName);
    }

    private static string GetRequestBody(this HttpRequest request)
    {
        if (!request.Body.CanRead) return null;

        if (!request.Body.CanSeek) return null;

        if (request.Body.Length < 1) return null;

        string bodyStr;
        // 启用倒带功能，就可以让 Request.Body 可以再次读取
        request.Body.Seek(0, SeekOrigin.Begin);
        using (var reader
               = new StreamReader(request.Body, Encoding.UTF8, true, 1024, true))
        {
            bodyStr = reader.ReadToEnd();
        }

        request.Body.Position = 0;
        return bodyStr;
    }
}
