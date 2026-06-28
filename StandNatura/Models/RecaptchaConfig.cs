using System;
using System.IO;
using System.Text.Json;

namespace StandNatura.Models
{
    public static class RecaptchaConfig
    {
        // Keys resolved once at runtime so they aren't committed to source. Order:
        //   1. STANDNATURA_RECAPTCHA_JSON environment variable (a file path).
        //   2. recaptcha.local.json next to the app (git-ignored).
        // The Site Key is public by design; the Secret Key is kept out of source
        // (though in a desktop app it still ships with the build — see plan notes).
        private static readonly Lazy<(string site, string secret)> _keys = new(Load);

        public static string SiteKey => _keys.Value.site;
        public static string SecretKey => _keys.Value.secret;
        public static bool IsConfigured =>
            !string.IsNullOrWhiteSpace(SiteKey) && !string.IsNullOrWhiteSpace(SecretKey);

        private static (string, string) Load()
        {
            string? path = ResolvePath();
            if (path == null)
                return (string.Empty, string.Empty);

            try
            {
                using var stream = File.OpenRead(path);
                using var doc = JsonDocument.Parse(stream);
                var root = doc.RootElement;
                string site = root.TryGetProperty("siteKey", out var s) ? s.GetString() ?? "" : "";
                string secret = root.TryGetProperty("secretKey", out var k) ? k.GetString() ?? "" : "";
                return (site.Trim(), secret.Trim());
            }
            catch
            {
                return (string.Empty, string.Empty);
            }
        }

        private static string? ResolvePath()
        {
            var fromEnv = Environment.GetEnvironmentVariable("STANDNATURA_RECAPTCHA_JSON");
            if (!string.IsNullOrWhiteSpace(fromEnv) && File.Exists(fromEnv))
                return fromEnv;

            var local = Path.Combine(AppContext.BaseDirectory, "recaptcha.local.json");
            return File.Exists(local) ? local : null;
        }
    }
}
