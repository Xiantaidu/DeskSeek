using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;

namespace DeskSeek.Services
{
    public class WebViewManager
    {
        private readonly WebView2 _webView;
        private readonly FrameworkElement? _loadingIndicator;
        private bool _isInitialized;

        public bool IsInitialized => _isInitialized;
        public CoreWebView2? Core => _webView.CoreWebView2;

        public event Action? NavigationStarted;
        public event Action? NavigationFinished;

        public WebViewManager(WebView2 webView, FrameworkElement? loadingIndicator = null)
        {
            _webView = webView;
            _loadingIndicator = loadingIndicator;
        }

        public async Task InitializeAsync(double initialZoom = 1.0)
        {
            if (_isInitialized) return;

            try
            {
                var profileDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "DeskSeek", "WebViewProfile");
                Directory.CreateDirectory(profileDir);

                // Configure performance flags to optimize memory and disable bloated background network tasks
                var options = new CoreWebView2EnvironmentOptions
                {
                    AdditionalBrowserArguments = "--disable-features=Translate,OptimizationHints,MediaRouter --disable-background-networking --disable-sync --disable-component-update --renderer-process-limit=1 --js-flags=\"--max-old-space-size=256\""
                };

                var env = await CoreWebView2Environment.CreateAsync(null, profileDir, options);
                await _webView.EnsureCoreWebView2Async(env);

                _isInitialized = true;
                _webView.DefaultBackgroundColor = System.Drawing.Color.Transparent;
                _webView.CoreWebView2.Settings.IsStatusBarEnabled = false;
                _webView.CoreWebView2.Settings.AreDevToolsEnabled = true;

                // Open external links in default system browser
                _webView.CoreWebView2.NewWindowRequested += (s, args) =>
                {
                    args.Handled = true;
                    try
                    {
                        Process.Start(new ProcessStartInfo(args.Uri) { UseShellExecute = true });
                    }
                    catch { }
                };

                // Navigation status
                _webView.NavigationStarting += (s, args) =>
                {
                    if (_loadingIndicator != null) _loadingIndicator.Visibility = Visibility.Visible;
                    NavigationStarted?.Invoke();
                };

                _webView.NavigationCompleted += (s, args) =>
                {
                    if (_loadingIndicator != null) _loadingIndicator.Visibility = Visibility.Collapsed;
                    try
                    {
                        _webView.ZoomFactor = initialZoom;
                    }
                    catch { }

                    // Inject custom scrollbar style for sleek sidebar
                    string script = @"
                        (function() {
                            if (document.getElementById('deskseek-custom-style')) return;
                            const style = document.createElement('style');
                            style.id = 'deskseek-custom-style';
                            style.textContent = `
                                ::-webkit-scrollbar { width: 6px; height: 6px; }
                                ::-webkit-scrollbar-thumb { background: rgba(0,0,0,0.18); border-radius: 3px; }
                                ::-webkit-scrollbar-thumb:hover { background: rgba(0,0,0,0.32); }
                            `;
                            document.head.appendChild(style);
                        })();
                    ";
                    _webView.CoreWebView2.ExecuteScriptAsync(script);

                    NavigationFinished?.Invoke();
                };

                _webView.Source = new Uri("https://chat.deepseek.com");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DeskSeek] WebView2 init error: {ex.Message}");
                if (_loadingIndicator != null) _loadingIndicator.Visibility = Visibility.Collapsed;
            }
        }

        public void SetZoom(double zoom)
        {
            if (_webView.CoreWebView2 != null)
            {
                try
                {
                    _webView.ZoomFactor = zoom;
                }
                catch { }
            }
        }

        public void Reload()
        {
            _webView.CoreWebView2?.Reload();
        }

        public void Resume()
        {
            if (_webView.CoreWebView2 != null)
            {
                try
                {
                    _webView.CoreWebView2.Resume();
                }
                catch { }
            }
        }

        public async Task TrySuspendAsync()
        {
            if (_webView.CoreWebView2 != null)
            {
                try
                {
                    await _webView.CoreWebView2.TrySuspendAsync();
                }
                catch { }
            }
        }

        public string GetCurrentUrl()
        {
            try
            {
                string? url = _webView.CoreWebView2?.Source;
                if (!string.IsNullOrWhiteSpace(url) && Uri.TryCreate(url, UriKind.Absolute, out var uri))
                {
                    if (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
                    {
                        return url;
                    }
                }

                if (_webView.Source != null)
                {
                    string fallback = _webView.Source.ToString();
                    if (fallback.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                        fallback.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                    {
                        return fallback;
                    }
                }
            }
            catch { }

            return "https://chat.deepseek.com";
        }

        public async Task<string> GetCurrentUrlAsync()
        {
            try
            {
                if (_webView.CoreWebView2 != null)
                {
                    string jsonUrl = await _webView.CoreWebView2.ExecuteScriptAsync("window.location.href");
                    if (!string.IsNullOrWhiteSpace(jsonUrl) && jsonUrl != "null")
                    {
                        string unquoted = System.Text.Json.JsonSerializer.Deserialize<string>(jsonUrl) ?? jsonUrl.Trim('"');
                        if (Uri.TryCreate(unquoted, UriKind.Absolute, out var uri) &&
                            (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
                        {
                            return unquoted;
                        }
                    }
                }
            }
            catch { }

            return GetCurrentUrl();
        }
    }
}
