using Microsoft.AspNetCore.Routing;

namespace LexiCraft.ApiGateway.Utilities;

/// <summary>
/// 路径匹配工具类
/// 用于匹配请求路径与配置的路径模式
/// </summary>
public static class PathMatcher
{
    /// <summary>
    /// 检查请求路径是否匹配指定的路径模式
    /// 支持通配符模式，如 "/api/*", "/users/{id}"
    /// </summary>
    /// <param name="requestPath">请求的实际路径</param>
    /// <param name="pathPattern">路径匹配模式</param>
    /// <returns>如果匹配则返回true，否则返回false</returns>
    public static bool Matches(string requestPath, string pathPattern)
    {
        // 处理通配符模式，例如 "/api/*"
        if (pathPattern.EndsWith("/*"))
        {
            var basePath = pathPattern.Substring(0, pathPattern.Length - 2); // 移除 "/*"
            return requestPath.StartsWith(basePath, StringComparison.OrdinalIgnoreCase);
        }

        // 处理精确匹配
        if (pathPattern.Equals(requestPath, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // 处理包含参数的路径模式，例如 "/users/{id}"
        if (pathPattern.Contains("{") && pathPattern.Contains("}"))
        {
            return MatchParameterizedPath(requestPath, pathPattern);
        }

        return false;
    }

    /// <summary>
    /// 匹配带参数的路径模式
    /// </summary>
    /// <param name="requestPath">请求的实际路径</param>
    /// <param name="pathPattern">带参数的路径模式</param>
    /// <returns>如果匹配则返回true，否则返回false</returns>
    private static bool MatchParameterizedPath(string requestPath, string pathPattern)
    {
        // 将路径分割成段
        var requestSegments = requestPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var patternSegments = pathPattern.Split('/', StringSplitOptions.RemoveEmptyEntries);

        if (requestSegments.Length != patternSegments.Length)
        {
            return false;
        }

        for (int i = 0; i < requestSegments.Length; i++)
        {
            var patternSegment = patternSegments[i];
            var requestSegment = requestSegments[i];

            // 如果模式段是参数形式（如{id}），则认为匹配
            if (patternSegment.StartsWith("{") && patternSegment.EndsWith("}"))
            {
                continue; // 参数段总是匹配
            }

            // 否则进行精确匹配
            if (!string.Equals(patternSegment, requestSegment, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        return true;
    }
}