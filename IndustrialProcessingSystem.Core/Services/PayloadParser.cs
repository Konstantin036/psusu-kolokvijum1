using System;
using System.Collections.Generic;
using System.Globalization;

namespace IndustrialProcessingSystem.Core.Services
{
    internal static class PayloadParser
    {
        public static (int Limit, int Threads) ParsePrimePayload(string payload)
        {
            var values = ParseKeyValuePayload(payload);
            int limit = ParseInt(values["numbers"]);
            int threads = Math.Clamp(ParseInt(values["threads"]), 1, 8);

            return (limit, threads);
        }

        public static int ParseIoDelay(string payload)
        {
            var values = ParseKeyValuePayload(payload);
            return ParseInt(values["delay"]);
        }

        private static int ParseInt(string value)
        {
            // XML primeri koriste 10_000 radi citljivosti, a int.Parse ocekuje 10000.
            return int.Parse(value.Replace("_", string.Empty), CultureInfo.InvariantCulture);
        }

        private static Dictionary<string, string> ParseKeyValuePayload(string payload)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var part in payload.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                var keyValue = part.Split(':', 2, StringSplitOptions.TrimEntries);
                if (keyValue.Length == 2)
                    result[keyValue[0]] = keyValue[1];
            }

            return result;
        }
    }
}
