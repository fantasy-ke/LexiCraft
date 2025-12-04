using System.Net;
using Microsoft.AspNetCore.Http;

namespace BuildingBlocks.Extensions;

public static class RequestIpUtility
{
    private static bool IsLocal(this HttpRequest req)
    {
        var connection = req.HttpContext.Connection;
        if (connection.RemoteIpAddress == null)
            return connection.RemoteIpAddress == null && connection.LocalIpAddress == null;
        return connection.LocalIpAddress != null
            ? connection.RemoteIpAddress.Equals(connection.LocalIpAddress)
            : IPAddress.IsLoopback(connection.RemoteIpAddress);

        // for in memory TestServer or when dealing with default connection info
    }


    private static T? GetHeaderValueAs<T>(HttpContext context, string headerName)
    {
        if (!(context.Request.Headers?.TryGetValue(headerName, out var values) ?? false)) return default;
        var rawValues = values.ToString();

        if (!string.IsNullOrWhiteSpace(rawValues))
            return (T)Convert.ChangeType(values.ToString(), typeof(T));

        return default;
    }

    private static List<string> SplitCsv(string? csvList)
    {
        if (string.IsNullOrWhiteSpace(csvList))
            return [];

        return csvList
            .TrimEnd(',')
            .Split(',')
            .AsEnumerable()
            .Select(s => s.Trim())
            .ToList();
    }

    extension(HttpContext context)
    {
        public string GetRequestIp()
        {
            var ip = SplitCsv(GetHeaderValueAs<string>(context, "X-Forwarded-For")).FirstOrDefault()!;

            if (string.IsNullOrWhiteSpace(ip))
                ip = SplitCsv(GetHeaderValueAs<string>(context, "X-Real-IP")).FirstOrDefault()!;

            if (string.IsNullOrWhiteSpace(ip) && context.Connection.RemoteIpAddress != null)
                ip = context.Connection.RemoteIpAddress.ToString();

            if (string.IsNullOrWhiteSpace(ip))
                ip = GetHeaderValueAs<string>(context, "REMOTE_ADDR") ?? "127.0.0.1";

            return ip;
        }

        public bool IsLocal()
        {
            return context.GetRequestIp() is "127.0.0.1" or "::1" || context.Request.IsLocal();
        }

        public string GetRequestProperty(string property)
        {
            return context.Request.Headers[property].ToString();
        }
    }
}