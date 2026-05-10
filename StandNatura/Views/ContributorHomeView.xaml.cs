using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace StandNatura.Views
{
    public partial class ContributorHomeView : UserControl
    {
        private bool _isMenuOpen = false;
        private const double MenuWidth = 320;
        private const int AnimationMs = 250;

        public ContributorHomeView()
        {
            InitializeComponent();
        }

        // ── Toggle the hamburger menu open/closed ────────────────────
        private void MenuToggleButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isMenuOpen)
                CloseMenu();
            else
                OpenMenu();
        }

        // ── Click outside the menu (on the dim overlay) closes it ────
        private void MenuDimOverlay_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (_isMenuOpen)
                CloseMenu();
        }

        // ── Clicking a menu item navigates AND closes the menu ───────
        // (the navigation itself is handled by the Command binding)
        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            CloseMenu();
        }

        // ── Open animation: slide panel in, fade dim overlay in ──────
        private void OpenMenu()
        {
            _isMenuOpen = true;

            // Enable the dim overlay so it can capture clicks
            MenuDimOverlay.IsHitTestVisible = true;

            // Slide the menu panel from -320 (off-screen) to 0
            var slideIn = new DoubleAnimation
            {
                From = -MenuWidth,
                To = 0,
                Duration = System.TimeSpan.FromMilliseconds(AnimationMs),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            // Fade the dim overlay from 0 to 0.5 opacity
            var fadeIn = new DoubleAnimation
            {
                From = 0,
                To = 0.5,
                Duration = System.TimeSpan.FromMilliseconds(AnimationMs)
            };

            ((TranslateTransform)MenuPanel.RenderTransform).BeginAnimation(
                TranslateTransform.XProperty, slideIn);
            MenuDimOverlay.BeginAnimation(OpacityProperty, fadeIn);
        }

        // ── Close animation: slide panel out, fade dim overlay out ───
        private void CloseMenu()
        {
            _isMenuOpen = false;

            var slideOut = new DoubleAnimation
            {
                From = 0,
                To = -MenuWidth,
                Duration = System.TimeSpan.FromMilliseconds(AnimationMs),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
            };

            var fadeOut = new DoubleAnimation
            {
                From = 0.5,
                To = 0,
                Duration = System.TimeSpan.FromMilliseconds(AnimationMs)
            };

            // Disable hit-testing on the overlay once it's faded out
            fadeOut.Completed += (s, e) => MenuDimOverlay.IsHitTestVisible = false;

            ((TranslateTransform)MenuPanel.RenderTransform).BeginAnimation(
                TranslateTransform.XProperty, slideOut);
            MenuDimOverlay.BeginAnimation(OpacityProperty, fadeOut);
        }
    }
}