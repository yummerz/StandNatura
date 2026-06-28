using System;
using StandNatura.Commands;
using StandNatura.Models;
using System.Windows.Input;
using System.Windows;
using Microsoft.Data.SqlClient;

namespace StandNatura.ViewModels
{
    public class AdminHomeViewModel : BaseViewModel
    {
        private readonly Action<BaseViewModel> _navigate;
        private readonly Action _onLogout;
        private readonly User _currentUser;
        private static readonly string connectionString = DatabaseConfig.ConnectionString;
        private bool _statsLoaded;

        // ── DASHBOARD STATS ───────────────────────────────────
        public string WelcomeMessage => "Welcome back, " + _currentUser.Username;

        public int PendingCount { get; private set; }
        public int TotalUsers { get; private set; }
        public decimal TotalDonations { get; private set; }

        // Shown as "—" if the stats query failed, so a load error never looks like a real 0.
        public string PendingCountDisplay   => _statsLoaded ? PendingCount.ToString() : "—";
        public string TotalUsersDisplay     => _statsLoaded ? TotalUsers.ToString() : "—";
        public string TotalDonationsDisplay => _statsLoaded ? $"₱{TotalDonations:N2}" : "—";

        public ICommand GoToVerifyPostsCommand { get; }
        public ICommand GoToManageUsersCommand { get; }
        public ICommand GoToSightingFeedCommand { get; }
        public ICommand GoToDonationsFundsCommand { get; }
        public ICommand GoToHiddenSightingsCommand { get; }
        public ICommand LogoutCommand { get; }

        public AdminHomeViewModel(Action<BaseViewModel> navigate, User currentUser, Action onLogout)
        {
            _navigate = navigate;
            _currentUser = currentUser;
            _onLogout = onLogout;

            GoToVerifyPostsCommand = new RelayCommand(() => _navigate(new VerifyPostsViewModel(_navigate, _currentUser, _onLogout)));
            GoToManageUsersCommand = new RelayCommand(() => _navigate(new ManageUsersViewModel(_navigate, _currentUser, _onLogout)));
            GoToSightingFeedCommand = new RelayCommand(() => _navigate(new SightingFeedViewModel(_navigate, _currentUser, _onLogout)));
            GoToDonationsFundsCommand = new RelayCommand(() => _navigate(new DonationsFundsViewModel(_navigate, _currentUser, _onLogout)));
            GoToHiddenSightingsCommand = new RelayCommand(() => _navigate(new HiddenSightingsViewModel(_navigate, _currentUser, _onLogout)));
            LogoutCommand = new RelayCommand(Logout);

            LoadStats();
        }

        // ── LOAD DASHBOARD STATS ──────────────────────────────
        private void LoadStats()
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    string query = @"
                        SELECT
                            (SELECT COUNT(*) FROM Sighting WHERE Status = 'Pending') AS PendingCount,
                            (SELECT COUNT(*) FROM Users)                            AS TotalUsers,
                            (SELECT ISNULL(SUM(Amount), 0) FROM Donation)           AS TotalDonations";
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        connection.Open();
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                PendingCount   = (int)reader["PendingCount"];
                                TotalUsers     = (int)reader["TotalUsers"];
                                TotalDonations = (decimal)reader["TotalDonations"];
                                _statsLoaded = true;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Quietly leave the stats showing "—"; never interrupt the admin with a dialog.
                System.Diagnostics.Debug.WriteLine("AdminHomeViewModel.LoadStats failed: " + ex);
            }
        }

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