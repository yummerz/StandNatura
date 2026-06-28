using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Web.WebView2.Core;
using StandNatura.Models;

namespace StandNatura.Views
{
    /// <summary>
    /// Modal popup that hosts the reCAPTCHA widget in a window large enough for
    /// image challenges. On success it captures the token and closes; the opener
    /// reads <see cref="Token"/> after ShowDialog returns.
    /// </summary>
    public partial class CaptchaWindow : Window
    {
        private HttpListener? _server;

        public string? Token { get; private set; }

        public CaptchaWindow()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            Closed += OnClosed;
        }

        private async void OnLoaded(object? sender, RoutedEventArgs e)
        {
            try
            {
                await CaptchaWebView.EnsureCoreWebView2Async();
                CaptchaWebView.CoreWebView2.WebMessageReceived += OnMessage;

                if (string.IsNullOrWhiteSpace(RecaptchaConfig.SiteKey))
                {
                    CaptchaWebView.NavigateToString(NoKeyHtml);
                    return;
                }

                // Served from http://localhost so reCAPTCHA's domain check matches
                // the registered "localhost" domain (a null origin would be rejected).
                string url = StartLocalServer(BuildHtml());
                CaptchaWebView.CoreWebView2.Navigate(url);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to load CAPTCHA: " + ex.Message);
            }
        }

        // Widget JS posts {type:'solved'|'expired'|'error', token?}.
        private void OnMessage(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            try
            {
                using var doc = JsonDocument.Parse(e.TryGetWebMessageAsString());
                string type = doc.RootElement.GetProperty("type").GetString() ?? "";
                if (type == "solved" && doc.RootElement.TryGetProperty("token", out var t))
                {
                    Token = t.GetString();
                    Close(); // success -> close; the opener reads Token
                }
                // expired/error: leave the popup open so the user can retry.
            }
            catch { /* ignore malformed messages */ }
        }

        private void OnClosed(object? sender, EventArgs e)
        {
            if (_server != null)
            {
                try { _server.Stop(); _server.Close(); } catch { /* already gone */ }
                _server = null;
            }
            if (CaptchaWebView.CoreWebView2 != null)
                CaptchaWebView.CoreWebView2.WebMessageReceived -= OnMessage;
            CaptchaWebView.Dispose();
        }

        private static string BuildHtml() =>
            CaptchaHtmlTemplate.Replace("__SITE_KEY__", RecaptchaConfig.SiteKey);

        // Serves the widget HTML from http://localhost:<port>/ so the page's origin
        // hostname is literally "localhost" — matching the registered reCAPTCHA domain.
        private string StartLocalServer(string html)
        {
            int port = GetFreeLoopbackPort();
            string prefix = $"http://localhost:{port}/";

            _server = new HttpListener();
            _server.Prefixes.Add(prefix);
            _server.Start();

            byte[] body = Encoding.UTF8.GetBytes(html);
            HttpListener server = _server;
            _ = Task.Run(async () =>
            {
                while (server.IsListening)
                {
                    HttpListenerContext ctx;
                    try { ctx = await server.GetContextAsync(); }
                    catch { break; } // listener stopped/disposed
                    try
                    {
                        ctx.Response.ContentType = "text/html; charset=utf-8";
                        ctx.Response.ContentLength64 = body.Length;
                        await ctx.Response.OutputStream.WriteAsync(body);
                        ctx.Response.Close();
                    }
                    catch { /* client disconnected */ }
                }
            });

            return prefix;
        }

        private static int GetFreeLoopbackPort()
        {
            var probe = new TcpListener(IPAddress.Loopback, 0);
            probe.Start();
            int port = ((IPEndPoint)probe.LocalEndpoint).Port;
            probe.Stop();
            return port;
        }

        private const string NoKeyHtml = """
            <html><body style="font-family:sans-serif;background:#0D2818;color:#9BB39E;
            margin:0;display:flex;align-items:center;justify-content:center;height:100%;
            text-align:center;font-size:14px">
            reCAPTCHA not configured &mdash; add recaptcha.local.json next to the app.
            </body></html>
            """;

        private const string CaptchaHtmlTemplate = """
            <!DOCTYPE html>
            <html>
            <head>
              <meta charset="utf-8" />
              <script src="https://www.google.com/recaptcha/api.js" async defer></script>
              <style>
                html, body { margin: 0; background: #0D2818; }
                .wrap { display: flex; justify-content: center; padding-top: 24px; }
              </style>
            </head>
            <body>
              <div class="wrap">
                <div class="g-recaptcha"
                     data-sitekey="__SITE_KEY__"
                     data-theme="dark"
                     data-callback="onSolved"
                     data-expired-callback="onExpired"
                     data-error-callback="onError"></div>
              </div>
              <script>
                function post(obj) { window.chrome.webview.postMessage(JSON.stringify(obj)); }
                function onSolved(token) { post({ type: 'solved', token: token }); }
                function onExpired() { post({ type: 'expired' }); }
                function onError() { post({ type: 'error' }); }
              </script>
            </body>
            </html>
            """;
    }
}
