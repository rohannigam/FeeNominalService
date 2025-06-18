using System.Text.RegularExpressions;

namespace FeeNominalService.Services
{
    public static class EndpointMatcher
    {
        public static bool IsEndpointAllowed(string requestPath, string allowedPath)
        {
            if (string.IsNullOrEmpty(requestPath) || string.IsNullOrEmpty(allowedPath))
                return false;

            // Convert to lowercase for case-insensitive comparison
            requestPath = requestPath.ToLower();
            allowedPath = allowedPath.ToLower();

            // If the allowed path ends with *, treat it as a wildcard
            if (allowedPath.EndsWith("*"))
            {
                // Remove the * and any trailing slashes
                var basePath = allowedPath.TrimEnd('*', '/');
                return requestPath.StartsWith(basePath);
            }

            // Exact match
            return requestPath == allowedPath;
        }

        public static bool IsEndpointAllowed(string requestPath, IEnumerable<string> allowedPaths)
        {
            return allowedPaths.Any(path => IsEndpointAllowed(requestPath, path));
        }
    }
} 