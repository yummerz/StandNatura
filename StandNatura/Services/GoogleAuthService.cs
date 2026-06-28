using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Util.Store;
using StandNatura.Models;

namespace StandNatura.Services
{
    /// <summary>The signed-in Google user's basic profile.</summary>
    public class GoogleUser
    {
        public string Email { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
    }

    /// <summary>
    /// Runs the Google OAuth desktop (loopback) sign-in flow: opens the system
    /// browser, receives the redirect on a temporary local listener, exchanges the
    /// code for tokens, and returns the user's verified email from the ID token.
    /// Returns null if no ID token came back; throws if not configured.
    /// </summary>
    public static class GoogleAuthService
    {
        private static readonly string[] Scopes = { "openid", "email", "profile" };

        public static async Task<GoogleUser?> SignInAsync()
        {
            string? secretsPath = GoogleConfig.ResolveSecretsPath();
            if (secretsPath == null)
                throw new InvalidOperationException(
                    "Google sign-in is not configured. Create google.local.json next to the app " +
                    "(see google.local.json.example).");

            GoogleClientSecrets secrets;
            using (var stream = new FileStream(secretsPath, FileMode.Open, FileAccess.Read))
            {
                secrets = await GoogleClientSecrets.FromStreamAsync(stream);
            }

            // NullDataStore => tokens are not cached, so every click is a fresh
            // sign-in (predictable, and avoids stale/revoked cached credentials).
            var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                secrets.Secrets,
                Scopes,
                "user",
                CancellationToken.None,
                new NullDataStore());

            string? idToken = credential.Token.IdToken;
            if (string.IsNullOrEmpty(idToken))
                return null;

            // Validate the ID token (signature + expiry) and confirm it was issued
            // for OUR client id, then read the verified email/name.
            var settings = new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = new[] { secrets.Secrets.ClientId }
            };
            var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, settings);

            return new GoogleUser
            {
                Email = payload.Email ?? string.Empty,
                Name = payload.Name ?? string.Empty
            };
        }
    }
}
