using System;
using System.Collections.Generic;

namespace MobileForge.Infrastructure
{
    /// <summary>
    /// Route registration + URL parsing for push notifications and deep links.
    /// </summary>
    public class DeepLinkRouter
    {
        private readonly List<(string Pattern, Action<Dictionary<string, string>> Handler)> _routes = new();

        /// <summary>Register a route. Pattern uses {param} placeholders.</summary>
        public void Register(string pattern, Action<Dictionary<string, string>> handler)
        {
            _routes.Add((pattern, handler));
        }

        /// <summary>Handle a deep link URL. Returns true if matched.</summary>
        public bool Handle(string url)
        {
            var path = url;
            int schemeEnd = url.IndexOf("://", StringComparison.Ordinal);
            if (schemeEnd >= 0) path = url[(schemeEnd + 3)..];
            path = path.Trim().TrimStart('/').TrimEnd('/');

            foreach (var (pattern, handler) in _routes)
            {
                var paramDict = Match(pattern, path);
                if (paramDict != null)
                {
                    handler(paramDict);
                    return true;
                }
            }
            return false;
        }

        private static Dictionary<string, string> Match(string pattern, string path)
        {
            var patternParts = pattern.Split('/');
            var pathParts = path.Split('/');
            if (patternParts.Length != pathParts.Length) return null;

            var result = new Dictionary<string, string>();
            for (int i = 0; i < patternParts.Length; i++)
            {
                var pp = patternParts[i];
                if (pp.StartsWith("{") && pp.EndsWith("}"))
                    result[pp[1..^1]] = pathParts[i];
                else if (pp != pathParts[i])
                    return null;
            }
            return result;
        }
    }
}
