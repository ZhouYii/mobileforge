using System;
using System.Collections.Generic;

namespace MobileForge.Infrastructure
{
    /// <summary>
    /// HTTP response wrapper. Encapsulates status code, body, and error state.
    /// Engine-agnostic; actual HTTP transport is injected or platform-specific.
    /// </summary>
    public class Response
    {
        public int StatusCode { get; }
        public string Body { get; }
        public bool IsSuccess { get; }
        public string Error { get; }

        public Response(int statusCode, string body, string error = null)
        {
            StatusCode = statusCode;
            Body = body ?? "";
            Error = error;
            IsSuccess = statusCode >= 200 && statusCode < 300 && error == null;
        }

        /// <summary>
        /// Convenience factory for a successful response.
        /// </summary>
        public static Response Success(int statusCode, string body)
        {
            return new Response(statusCode, body);
        }

        /// <summary>
        /// Convenience factory for a failed response.
        /// </summary>
        public static Response Failure(int statusCode, string error, string body = null)
        {
            return new Response(statusCode, body, error);
        }

        /// <summary>
        /// Parse body as a JSON dictionary using MiniJSON-style deserialization.
        /// This is a minimal helper; callers should provide their own JSON parser
        /// or use Unity's JsonUtility in the engine layer.
        /// Returns null if parsing fails.
        /// </summary>
        public Dictionary<string, object> ParseJson()
        {
            if (string.IsNullOrEmpty(Body))
                return null;

            try
            {
                // Minimal inline JSON parser for flat key-value pairs.
                // For production, replace with MiniJSON, Newtonsoft, or Unity's JsonUtility.
                return SimpleJsonParse(Body);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Very simple top-level JSON object parser for flat string/number values.
        /// Not a full JSON parser; intended as a bootstrap until a real parser is wired in.
        /// </summary>
        private static Dictionary<string, object> SimpleJsonParse(string json)
        {
            var result = new Dictionary<string, object>();
            json = json.Trim();

            if (json.Length < 2 || json[0] != '{' || json[json.Length - 1] != '}')
                return result;

            // Strip outer braces
            json = json.Substring(1, json.Length - 2).Trim();
            if (string.IsNullOrEmpty(json))
                return result;

            // Split by comma (does not handle nested objects/arrays)
            var pairs = json.Split(',');
            foreach (var pair in pairs)
            {
                var colonIdx = pair.IndexOf(':');
                if (colonIdx < 0) continue;

                var key = pair.Substring(0, colonIdx).Trim().Trim('"');
                var rawValue = pair.Substring(colonIdx + 1).Trim();

                if (rawValue.StartsWith("\"") && rawValue.EndsWith("\""))
                {
                    result[key] = rawValue.Substring(1, rawValue.Length - 2);
                }
                else if (rawValue == "true")
                {
                    result[key] = true;
                }
                else if (rawValue == "false")
                {
                    result[key] = false;
                }
                else if (rawValue == "null")
                {
                    result[key] = null;
                }
                else if (int.TryParse(rawValue, out var intVal))
                {
                    result[key] = intVal;
                }
                else if (float.TryParse(rawValue, System.Globalization.NumberStyles.Float,
                             System.Globalization.CultureInfo.InvariantCulture, out var floatVal))
                {
                    result[key] = floatVal;
                }
                else
                {
                    result[key] = rawValue;
                }
            }

            return result;
        }

        public override string ToString()
        {
            return IsSuccess
                ? $"Response({StatusCode} OK, {Body.Length} chars)"
                : $"Response({StatusCode} ERROR: {Error})";
        }
    }
}
