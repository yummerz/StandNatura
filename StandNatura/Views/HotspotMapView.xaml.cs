using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Web.WebView2.Wpf;
using StandNatura.Models;
using StandNatura.ViewModels;

namespace StandNatura.Views
{
    /// <summary>
    /// Interaction logic for HotspotMapView.xaml
    /// </summary>
    public partial class HotspotMapView : UserControl
    {
        private WebView2? _mapWebView;

        public HotspotMapView()
        {
            InitializeComponent();
            Shell.MenuOpened += OnShellMenuOpened;
            Shell.MenuClosed += OnShellMenuClosed;
        }

        // WebView2 is a native (airspace) surface that renders above WPF content,
        // so hide it while the drawer is open or it covers the menu and steals clicks.
        private void OnShellMenuOpened(object? sender, EventArgs e)
        {
            if (_mapWebView != null)
                _mapWebView.Visibility = Visibility.Collapsed;
        }

        private void OnShellMenuClosed(object? sender, EventArgs e)
        {
            if (_mapWebView != null)
                _mapWebView.Visibility = Visibility.Visible;
        }

        private async void HotspotMapView_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_mapWebView == null)
                {
                    _mapWebView = new WebView2();
                    var host = new Grid
                    {
                        Background = new SolidColorBrush(
                            (Color)ColorConverter.ConvertFromString("#1B4332"))
                    };
                    host.Children.Add(_mapWebView);
                    Shell.PageContent = host;
                }

                await _mapWebView.EnsureCoreWebView2Async();
                _mapWebView.NavigateToString(BuildMapHtml());
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to load map: " + ex.Message);
            }
        }

        private void HotspotMapView_Unloaded(object sender, RoutedEventArgs e)
        {
            Shell.MenuOpened -= OnShellMenuOpened;
            Shell.MenuClosed -= OnShellMenuClosed;
            _mapWebView?.Dispose();
            _mapWebView = null;
        }

        private string BuildMapHtml()
        {
            string apiKey = MapsConfig.ApiKey;
            if (string.IsNullOrWhiteSpace(apiKey))
                return NoKeyHtml;

            string pinsJson = (DataContext as HotspotMapViewModel)?.PinsJson ?? "[]";
            return MapHtmlTemplate
                .Replace("__API_KEY__", apiKey)
                .Replace("__PINS__", pinsJson);
        }

        private const string NoKeyHtml = """
            <html><body style="font-family:sans-serif;background:#1B4332;color:#D8E5DA;
            display:flex;align-items:center;justify-content:center;height:100%;text-align:center">
            <div><h2>Google Maps key not configured</h2>
            <p>Create <code>maps.local.txt</code> next to the app with your API key.<br>
            See <code>maps.local.txt.example</code>.</p></div></body></html>
            """;

        private const string MapHtmlTemplate = """
            <!DOCTYPE html>
            <html>
            <head>
              <meta charset="utf-8" />
              <style>
                html, body { height: 100%; margin: 0; padding: 0;
                  font-family: 'Trebuchet MS', 'Segoe UI', sans-serif; }
                #layout { display: flex; height: 100%; background: #1B4332; }

                /* ===== SIDE PANEL (sighting list) ===== */
                #sidebar { width: 300px; flex: 0 0 300px; background: #0D2818;
                  overflow-y: auto; box-sizing: border-box;
                  border-right: 1px solid rgba(95,165,114,0.3); }
                #sidebar-header { padding: 18px 18px 4px; color: #F5F0E8;
                  font-size: 18px; font-weight: 600; }
                #sidebar-sub { padding: 0 18px 12px; color: #9BB39E;
                  font-size: 12px; font-style: italic; }
                .card { margin: 8px 12px; padding: 12px 14px;
                  background: rgba(31,64,48,0.85);
                  border: 1px solid rgba(95,165,114,0.3); border-radius: 10px;
                  cursor: pointer; box-shadow: 0 2px 10px rgba(0,0,0,0.35);
                  transition: border-color .15s, background .15s; }
                .card:hover { border-color: rgba(116,198,157,0.6); }
                .card.active { border-color: #74C69D; background: rgba(45,106,79,0.9);
                  box-shadow: 0 0 0 1px #74C69D, 0 4px 14px rgba(0,0,0,0.5); }
                .card .title { color: #F5F0E8; font-size: 15px; font-weight: 600;
                  margin-bottom: 4px; }
                .card .loc { color: #9BB39E; font-size: 12px; }
                .empty { padding: 20px 18px; color: #9BB39E; font-size: 13px;
                  font-style: italic; }

                /* ===== MAP + FLOATING TOOLBAR ===== */
                #mapWrap { position: relative; flex: 1 1 auto; }
                #map { position: absolute; inset: 0; }
                #toolbar { position: absolute; top: 12px; left: 50%;
                  transform: translateX(-50%); z-index: 5;
                  background: rgba(13,40,24,0.95);
                  border: 1px solid rgba(95,165,114,0.5); border-radius: 20px;
                  padding: 8px 18px; color: #F5F0E8; font-size: 13px; font-weight: 600;
                  box-shadow: 0 2px 12px rgba(0,0,0,0.5); white-space: nowrap; }
              </style>
            </head>
            <body>
              <div id="layout">
                <aside id="sidebar">
                  <div id="sidebar-header">Approved Sightings</div>
                  <div id="sidebar-sub">Sanctuaries discovered by our community</div>
                  <div id="list"></div>
                </aside>
                <div id="mapWrap">
                  <div id="toolbar">🌿 <span id="count">0</span> Approved Sightings</div>
                  <div id="map"></div>
                </div>
              </div>

              <script>
                const PINS = __PINS__;

                // ===== DARK MAP THEME (separate from navigation fix) =====
                // Tuned to the app's dark-green palette so the map blends in.
                const DARK_MAP_STYLE = [
                  { elementType: "geometry", stylers: [{ color: "#1b4332" }] },
                  { elementType: "labels.text.stroke", stylers: [{ color: "#0d2818" }] },
                  { elementType: "labels.text.fill", stylers: [{ color: "#9bb39e" }] },
                  { featureType: "administrative", elementType: "geometry",
                    stylers: [{ color: "#5fa572" }, { weight: 0.5 }] },
                  { featureType: "administrative.country", elementType: "labels.text.fill",
                    stylers: [{ color: "#d8e5da" }] },
                  { featureType: "administrative.locality", elementType: "labels.text.fill",
                    stylers: [{ color: "#95d5b2" }] },
                  { featureType: "poi", elementType: "labels.text.fill",
                    stylers: [{ color: "#74c69d" }] },
                  { featureType: "poi.park", elementType: "geometry",
                    stylers: [{ color: "#2d6a4f" }] },
                  { featureType: "poi.park", elementType: "labels.text.fill",
                    stylers: [{ color: "#95d5b2" }] },
                  { featureType: "road", elementType: "geometry",
                    stylers: [{ color: "#2d6a4f" }] },
                  { featureType: "road", elementType: "labels.text.fill",
                    stylers: [{ color: "#9bb39e" }] },
                  { featureType: "road.highway", elementType: "geometry",
                    stylers: [{ color: "#40916c" }] },
                  { featureType: "transit", elementType: "labels.text.fill",
                    stylers: [{ color: "#74c69d" }] },
                  { featureType: "water", elementType: "geometry",
                    stylers: [{ color: "#0d2818" }] },
                  { featureType: "water", elementType: "labels.text.fill",
                    stylers: [{ color: "#52796f" }] }
                ];
                // ===== END DARK MAP THEME =====

                // ===== CUSTOM LEAF PIN (branding marker) =====
                // A dark-green teardrop with a pale leaf, echoing the header logo.
                const LEAF_SVG =
                  '<svg xmlns="http://www.w3.org/2000/svg" width="40" height="48" viewBox="0 0 40 48">' +
                  '<path d="M20 1 C11 1 4 8 4 17 C4 29 20 47 20 47 C20 47 36 29 36 17 C36 8 29 1 20 1 Z" ' +
                  'fill="#1B4332" stroke="#5FA572" stroke-width="2"/>' +
                  '<path d="M20 8 C14 11 12 18 15 24 C21 22 24 15 23 9 C22 8.4 21 8 20 8 Z" fill="#95D5B2"/>' +
                  '<path d="M15 23 C18 18 21 13 22 10" stroke="#1B4332" stroke-width="1.3" ' +
                  'fill="none" stroke-linecap="round"/>' +
                  '</svg>';
                function leafIcon() {
                  return {
                    url: 'data:image/svg+xml;charset=UTF-8,' + encodeURIComponent(LEAF_SVG),
                    scaledSize: new google.maps.Size(40, 48),
                    anchor: new google.maps.Point(20, 46)
                  };
                }
                // ===== END CUSTOM LEAF PIN =====

                function escapeHtml(s) {
                  return (s || '').replace(/[&<>"']/g, c =>
                    ({ '&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;' }[c]));
                }

                let map, info;
                const markers = [];

                // Shared selection: highlights the list card and (when the map is
                // ready) pans to the pin and opens its info window. Called from both
                // a list-card click and a marker click, so the two stay in sync.
                function selectSighting(i) {
                  const p = PINS[i];
                  if (!p) return;

                  document.querySelectorAll('#list .card').forEach((c, idx) =>
                    c.classList.toggle('active', idx === i));
                  const activeCard = document.querySelector('#list .card[data-i="' + i + '"]');
                  if (activeCard) activeCard.scrollIntoView({ behavior: 'smooth', block: 'nearest' });

                  if (!map) return;
                  map.panTo({ lat: p.lat, lng: p.lng });
                  if (map.getZoom() < 8) map.setZoom(8);
                  info.setContent(
                    '<div style="font-family:sans-serif;min-width:120px">' +
                    '<strong>' + escapeHtml(p.title) + '</strong><br>' +
                    escapeHtml(p.location) + '</div>');
                  info.open(map, markers[i]);
                }

                function buildList() {
                  const list = document.getElementById('list');
                  document.getElementById('count').textContent = PINS.length;
                  if (!PINS.length) {
                    list.innerHTML = '<div class="empty">No approved sightings yet.</div>';
                    return;
                  }
                  PINS.forEach((p, i) => {
                    const card = document.createElement('div');
                    card.className = 'card';
                    card.setAttribute('data-i', i);
                    card.innerHTML =
                      '<div class="title">' + escapeHtml(p.title) + '</div>' +
                      '<div class="loc">\u{1F4CD} ' + escapeHtml(p.location) + '</div>';
                    card.addEventListener('click', () => selectSighting(i));
                    list.appendChild(card);
                  });
                }

                function initMap() {
                  const phCenter = { lat: 12.8797, lng: 121.7740 };
                  map = new google.maps.Map(document.getElementById('map'), {
                    zoom: PINS.length ? 6 : 5,
                    center: PINS.length ? { lat: PINS[0].lat, lng: PINS[0].lng } : phCenter,
                    styles: DARK_MAP_STYLE
                  });
                  info = new google.maps.InfoWindow();
                  const icon = leafIcon();
                  PINS.forEach((p, i) => {
                    const marker = new google.maps.Marker({
                      position: { lat: p.lat, lng: p.lng }, map, title: p.title, icon: icon
                    });
                    marker.addListener('click', () => selectSighting(i));
                    markers.push(marker);
                  });
                }

                // Build the sidebar immediately so the list + count show even while
                // the map tiles load. Markers are created later in initMap.
                window.addEventListener('DOMContentLoaded', buildList);
              </script>
              <script async
                src="https://maps.googleapis.com/maps/api/js?key=__API_KEY__&callback=initMap">
              </script>
            </body>
            </html>
            """;
    }
}
