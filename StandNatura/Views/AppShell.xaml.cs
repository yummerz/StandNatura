using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace StandNatura.Views
{
    public partial class AppShell : UserControl
    {
        private bool _isMenuOpen = false;
        private const double MenuWidth = 320;
        private const int AnimationMs = 250;

        // ── PageContent: the host view's content ──────────────
        public static readonly DependencyProperty PageContentProperty =
            DependencyProperty.Register(
                nameof(PageContent),
                typeof(object),
                typeof(AppShell),
                new PropertyMetadata(null));

        public object PageContent
        {
            get => GetValue(PageContentProperty);
            set => SetValue(PageContentProperty, value);
        }

        // ── ActivePageKey: which menu item to highlight ───────
        public static readonly DependencyProperty ActivePageKeyProperty =
            DependencyProperty.Register(
                nameof(ActivePageKey),
                typeof(string),
                typeof(AppShell),
                new PropertyMetadata(string.Empty, OnActivePageKeyChanged));

        public string ActivePageKey
        {
            get => (string)GetValue(ActivePageKeyProperty);
            set => SetValue(ActivePageKeyProperty, value);
        }

        // Triggered whenever the host sets ActivePageKey
        private static void OnActivePageKeyChanged(
            DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is AppShell shell)
                shell.UpdateActiveItemHighlight((string)e.NewValue);
        }

        // ── CONSTRUCTOR ───────────────────────────────────────
        public AppShell()
        {
            InitializeComponent();
        }

        // ── ACTIVE ITEM HIGHLIGHTING ──────────────────────────
        private void UpdateActiveItemHighlight(string activeKey)
        {
            // Reset all menu items to inactive
            ResetMenuItem(MenuItem_Home_Accent, MenuItem_Home_Text);
            ResetMenuItem(MenuItem_Submit_Accent, MenuItem_Submit_Text);
            ResetMenuItem(MenuItem_MySightings_Accent, MenuItem_MySightings_Text);
            ResetMenuItem(MenuItem_HotspotMap_Accent, MenuItem_HotspotMap_Text);

            // Activate the matching item
            switch (activeKey)
            {
                case "Home":
                    ActivateMenuItem(MenuItem_Home_Accent, MenuItem_Home_Text);
                    break;
                case "Submit":
                    ActivateMenuItem(MenuItem_Submit_Accent, MenuItem_Submit_Text);
                    break;
                case "MySightings":
                    ActivateMenuItem(MenuItem_MySightings_Accent, MenuItem_MySightings_Text);
                    break;
                case "HotspotMap":
                    ActivateMenuItem(MenuItem_HotspotMap_Accent, MenuItem_HotspotMap_Text);
                    break;
            }
        }

        private void ResetMenuItem(System.Windows.Shapes.Rectangle accent, TextBlock text)
        {
            accent.Visibility = Visibility.Collapsed;
            text.FontWeight = FontWeights.Normal;
            text.Foreground = new SolidColorBrush(
                (Color)ColorConverter.ConvertFromString("#D8E5DA"));
        }

        private void ActivateMenuItem(System.Windows.Shapes.Rectangle accent, TextBlock text)
        {
            accent.Visibility = Visibility.Visible;
            text.FontWeight = FontWeights.SemiBold;
            text.Foreground = new SolidColorBrush(
                (Color)ColorConverter.ConvertFromString("#F5F0E8"));
        }

        // ── MENU TOGGLE ───────────────────────────────────────
        private void MenuToggleButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isMenuOpen)
                CloseMenu();
            else
                OpenMenu();
        }

        private void MenuDimOverlay_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (_isMenuOpen)
                CloseMenu();
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            CloseMenu();
        }

        // ── ANIMATIONS ────────────────────────────────────────
        private void OpenMenu()
        {
            _isMenuOpen = true;
            MenuDimOverlay.IsHitTestVisible = true;

            var slideIn = new DoubleAnimation
            {
                From = -MenuWidth,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(AnimationMs),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            var fadeIn = new DoubleAnimation
            {
                From = 0,
                To = 0.5,
                Duration = TimeSpan.FromMilliseconds(AnimationMs)
            };

            ((TranslateTransform)MenuPanel.RenderTransform).BeginAnimation(
                TranslateTransform.XProperty, slideIn);
            MenuDimOverlay.BeginAnimation(OpacityProperty, fadeIn);
        }

        private void CloseMenu()
        {
            _isMenuOpen = false;

            var slideOut = new DoubleAnimation
            {
                From = 0,
                To = -MenuWidth,
                Duration = TimeSpan.FromMilliseconds(AnimationMs),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
            };

            var fadeOut = new DoubleAnimation
            {
                From = 0.5,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(AnimationMs)
            };

            fadeOut.Completed += (s, e) => MenuDimOverlay.IsHitTestVisible = false;

            ((TranslateTransform)MenuPanel.RenderTransform).BeginAnimation(
                TranslateTransform.XProperty, slideOut);
            MenuDimOverlay.BeginAnimation(OpacityProperty, fadeOut);
        }
    }
}