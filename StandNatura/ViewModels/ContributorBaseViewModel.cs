using System;
using System.Windows;
using System.Windows.Input;
using StandNatura.Commands;
using StandNatura.Models;

namespace StandNatura.ViewModels
{
    /// <summary>
    /// Base class for all contributor-side ViewModels.
    /// Provides shared navigation commands and user context so that
    /// the reusable AppShell (and its hamburger menu) can bind to
    /// these commands consistently from any contributor view.
    /// </summary>
    public abstract class ContributorBaseViewModel : BaseViewModel
    {
        // ── PROTECTED CONTEXT ─────────────────────────────────
        // Stored as protected so derived classes can pass them
        // along when navigating to peer views.
        protected readonly Action<BaseViewModel> _navigate;
        protected readonly User _currentUser;
        protected readonly Action _onLogout;

        // ── PUBLIC CONTEXT ────────────────────────────────────
        // Exposed for the AppShell's welcome label.
        public User CurrentUser => _currentUser;
        public string WelcomeMessage => _currentUser.Username;

        // ── ACTIVE PAGE KEY ───────────────────────────────────
        // Each derived view sets this so the AppShell knows which
        // menu item to highlight as active.
        // Valid values: "Home", "Submit", "MySightings", "HotspotMap"
        public abstract string ActivePageKey { get; }

        // ── SHARED NAVIGATION COMMANDS ────────────────────────
        public ICommand GoToHomeCommand { get; }
        public ICommand GoToSubmitSightingCommand { get; }
        public ICommand GoToMySightingsCommand { get; }
        public ICommand GoToHotspotMapCommand { get; }
        public ICommand LogoutCommand { get; }

        // ── CONSTRUCTOR ───────────────────────────────────────
        protected ContributorBaseViewModel(
            Action<BaseViewModel> navigate,
            User currentUser,
            Action onLogout)
        {
            _navigate = navigate;
            _currentUser = currentUser;
            _onLogout = onLogout;

            GoToHomeCommand = new RelayCommand(
                () => _navigate(new ContributorHomeViewModel(_navigate, _currentUser, _onLogout)));

            GoToSubmitSightingCommand = new RelayCommand(
                () => _navigate(new SubmitSightingViewModel(_navigate, _currentUser, _onLogout)));

            GoToMySightingsCommand = new RelayCommand(
                () => _navigate(new MySightingsViewModel(_navigate, _currentUser, _onLogout)));

            GoToHotspotMapCommand = new RelayCommand(
                () => _navigate(new HotspotMapViewModel(_navigate, _currentUser, _onLogout)));

            LogoutCommand = new RelayCommand(Logout);
        }

        // ── LOGOUT ────────────────────────────────────────────
        private void Logout()
        {
            var result = MessageBox.Show(
                "Are you sure you want to logout?",
                "Confirm Logout",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
                _onLogout();
        }
    }
}