using System;
using System.IO;
using System.Linq;

namespace StandNatura.Models
{
    public static class MapsConfig
    {
        // Resolved at runtime so the key is never committed to source.
        //   1. STANDNATURA_MAPS_KEY environment variable, if set.
        //   2. maps.local.txt next to the app (git-ignored).
        //   3. empty -> the map page shows a "key not configured" message.
        public static readonly string ApiKey = ResolveApiKey();

        private static string ResolveApiKey()
        {
            var fromEnv = Environment.GetEnvironmentVariable("STANDNATURA_MAPS_KEY");
            if (!string.IsNullOrWhiteSpace(fromEnv))
                return fromEnv.Trim();

            var localFile = Path.Combine(AppContext.BaseDirectory, "maps.local.txt");
            if (File.Exists(localFile))
            {
                var key = string.Join("", File.ReadAllLines(localFile)
                    .Where(l => !string.IsNullOrWhiteSpace(l) && !l.TrimStart().StartsWith("#")))
                    .Trim();
                if (!string.IsNullOrWhiteSpace(key))
                    return key;
            }
            return string.Empty;
        }
    }
}
