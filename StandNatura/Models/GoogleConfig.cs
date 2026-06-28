using System;
using System.IO;

namespace StandNatura.Models
{
    public static class GoogleConfig
    {
        // Path to the OAuth client JSON downloaded from Google Cloud, resolved at
        // runtime so the client secret is never committed to source. Order:
        //   1. STANDNATURA_GOOGLE_JSON environment variable, if set.
        //   2. google.local.json next to the app (git-ignored).
        // Returns null when no config file is present.
        public static string? ResolveSecretsPath()
        {
            var fromEnv = Environment.GetEnvironmentVariable("STANDNATURA_GOOGLE_JSON");
            if (!string.IsNullOrWhiteSpace(fromEnv) && File.Exists(fromEnv))
                return fromEnv;

            var localFile = Path.Combine(AppContext.BaseDirectory, "google.local.json");
            return File.Exists(localFile) ? localFile : null;
        }

        public static bool IsConfigured => ResolveSecretsPath() != null;
    }
}
