using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using StandNatura.Models;

namespace StandNatura.Services
{
    /// <summary>
    /// Validates a reCAPTCHA token with Google's siteverify endpoint. In this
    /// desktop app the C# process plays the "server" role (there is no backend),
    /// so the Secret Key is used here directly — see the plan notes on why this is
    /// bot friction rather than a hard security boundary.
    /// </summary>
    public static class RecaptchaService
    {
        private static readonly HttpClient Http = new HttpClient();
        private const string VerifyUrl = "https://www.google.com/recaptcha/api/siteverify";

        public static async Task<bool> VerifyAsync(string? token)
        {
            if (string.IsNullOrWhiteSpace(token) || !RecaptchaConfig.IsConfigured)
                return false;

            var form = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["secret"] = RecaptchaConfig.SecretKey,
                ["response"] = token
            });

            using var resp = await Http.PostAsync(VerifyUrl, form);
            resp.EnsureSuccessStatusCode();
            string json = await resp.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.TryGetProperty("success", out var ok) && ok.GetBoolean();
        }
    }
}
