using System;
using System.Collections.Generic;
using System.Text;

namespace MobileForge.Infrastructure
{
    /// <summary>
    /// Save file envelope with version, timestamp, checksum, and data payload.
    /// Provides creation, validation, extraction, and optional XOR encryption.
    /// </summary>
    public static class SaveFormat
    {
        private const string EncryptedPrefix = "MF_ENC:";
        public const string KeyVersion = "save_version";
        public const string KeyTimestamp = "save_timestamp";
        public const string KeyChecksum = "save_checksum";
        public const string KeyData = "save_data";

        /// <summary>
        /// Create a save envelope wrapping the given data.
        /// </summary>
        /// <param name="version">Save format version (e.g., 1).</param>
        /// <param name="data">The actual save data.</param>
        /// <returns>Envelope dictionary containing version, timestamp, checksum, and data.</returns>
        public static Dictionary<string, object> CreateEnvelope(int version, Dictionary<string, object> data)
        {
            data ??= new Dictionary<string, object>();

            var envelope = new Dictionary<string, object>
            {
                [KeyVersion] = version,
                [KeyTimestamp] = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                [KeyData] = data,
            };

            envelope[KeyChecksum] = ComputeChecksum(version, data);
            return envelope;
        }

        /// <summary>
        /// Validate an envelope's integrity. Checks for required keys and checksum match.
        /// </summary>
        /// <returns>True if the envelope is structurally valid and checksum matches.</returns>
        public static bool ValidateEnvelope(Dictionary<string, object> envelope)
        {
            if (envelope == null) return false;
            if (!envelope.ContainsKey(KeyVersion)) return false;
            if (!envelope.ContainsKey(KeyTimestamp)) return false;
            if (!envelope.ContainsKey(KeyChecksum)) return false;
            if (!envelope.ContainsKey(KeyData)) return false;

            var version = Convert.ToInt32(envelope[KeyVersion]);
            var data = envelope[KeyData] as Dictionary<string, object>;
            if (data == null) return false;

            var storedChecksum = Convert.ToString(envelope[KeyChecksum]);
            var computedChecksum = ComputeChecksum(version, data);

            return storedChecksum == computedChecksum;
        }

        /// <summary>
        /// Get the version from an envelope.
        /// </summary>
        public static int GetVersion(Dictionary<string, object> envelope)
        {
            if (envelope == null || !envelope.ContainsKey(KeyVersion))
                return -1;

            return Convert.ToInt32(envelope[KeyVersion]);
        }

        /// <summary>
        /// Get the data payload from an envelope.
        /// </summary>
        public static Dictionary<string, object> GetData(Dictionary<string, object> envelope)
        {
            if (envelope == null || !envelope.ContainsKey(KeyData))
                return null;

            return envelope[KeyData] as Dictionary<string, object>;
        }

        /// <summary>
        /// Get the timestamp (unix seconds) from an envelope.
        /// </summary>
        public static long GetTimestamp(Dictionary<string, object> envelope)
        {
            if (envelope == null || !envelope.ContainsKey(KeyTimestamp))
                return 0;

            return Convert.ToInt64(envelope[KeyTimestamp]);
        }

        // ── Encryption ──

        /// <summary>Encrypt a string using XOR obfuscation. Returns prefixed base64.</summary>
        public static string EncryptData(string plaintext, string key)
        {
            if (string.IsNullOrEmpty(key)) return plaintext;
            var data = Encoding.UTF8.GetBytes(plaintext);
            var keyBytes = Encoding.UTF8.GetBytes(key);
            var encrypted = new byte[data.Length];
            for (int i = 0; i < data.Length; i++)
                encrypted[i] = (byte)(data[i] ^ keyBytes[i % keyBytes.Length]);
            return EncryptedPrefix + Convert.ToBase64String(encrypted);
        }

        /// <summary>Decrypt a string encrypted with EncryptData. Returns plaintext. Non-encrypted strings pass through.</summary>
        public static string DecryptData(string encrypted, string key)
        {
            if (!IsEncrypted(encrypted)) return encrypted;
            var base64 = encrypted.Substring(EncryptedPrefix.Length);
            var data = Convert.FromBase64String(base64);
            var keyBytes = Encoding.UTF8.GetBytes(key);
            var decrypted = new byte[data.Length];
            for (int i = 0; i < data.Length; i++)
                decrypted[i] = (byte)(data[i] ^ keyBytes[i % keyBytes.Length]);
            return Encoding.UTF8.GetString(decrypted);
        }

        /// <summary>Check if a string has the encrypted prefix.</summary>
        public static bool IsEncrypted(string data) =>
            data != null && data.StartsWith(EncryptedPrefix, StringComparison.Ordinal);

        /// <summary>
        /// Compute a simple checksum for integrity validation.
        /// Uses a basic hash of the version + sorted key names + value count.
        /// Not cryptographic — just tamper detection.
        /// </summary>
        private static string ComputeChecksum(int version, Dictionary<string, object> data)
        {
            var sb = new StringBuilder();
            sb.Append("v");
            sb.Append(version);
            sb.Append("|");

            var keys = new List<string>(data.Keys);
            keys.Sort(StringComparer.Ordinal);

            foreach (var key in keys)
            {
                sb.Append(key);
                sb.Append("=");
                sb.Append(data[key]?.GetHashCode() ?? 0);
                sb.Append(";");
            }

            // Simple hash: FNV-1a 32-bit
            uint hash = 2166136261;
            foreach (char c in sb.ToString())
            {
                hash ^= c;
                hash *= 16777619;
            }

            return hash.ToString("x8");
        }
    }
}
