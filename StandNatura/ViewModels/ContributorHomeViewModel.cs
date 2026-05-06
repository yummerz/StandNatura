using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows;
using StandNatura.Commands;
using StandNatura.Models;
using Microsoft.Data.SqlClient;

namespace StandNatura.ViewModels
{
    public class ContributorHomeViewModel : BaseViewModel
    {
        private readonly Action<BaseViewModel> _navigate;
        private readonly User _currentUser;
        private readonly Action _onLogout;
        private static readonly string connectionString = DatabaseConfig.ConnectionString;

        // ── BINDABLE PROPERTIES ──────────────────────────────
        public string WelcomeMessage => $"👋 {_currentUser.Username}";

        private ObservableCollection<SightingDisplay> _sightings = new();
        public ObservableCollection<SightingDisplay> Sightings
        {
            get => _sightings;
            set => SetProperty(ref _sightings, value);
        }

        private SightingDisplay? _selectedSighting;
        public SightingDisplay? SelectedSighting
        {
            get => _selectedSighting;
            set => SetProperty(ref _selectedSighting, value);
        }

        // ── COMMANDS ──────────────────────────────────────────
        public ICommand GoToSubmitSightingCommand { get; }
        public ICommand GoToMySightingsCommand { get; }
        public ICommand GoToHotspotMapCommand { get; }
        public ICommand LogoutCommand { get; }
        public ICommand ViewSightingCommand { get; }

        // ── CONSTRUCTOR ───────────────────────────────────────
        public ContributorHomeViewModel(Action<BaseViewModel> navigate, User currentUser, Action onLogout)
        {
            _navigate = navigate;
            _currentUser = currentUser;
            _onLogout = onLogout;

            GoToSubmitSightingCommand = new RelayCommand(() => _navigate(new SubmitSightingViewModel(_navigate, _currentUser, _onLogout)));
            GoToMySightingsCommand = new RelayCommand(() => _navigate(new MySightingsViewModel(_navigate, _currentUser, _onLogout)));
            GoToHotspotMapCommand = new RelayCommand(() => _navigate(new HotspotMapViewModel(_navigate, _currentUser, _onLogout)));
            LogoutCommand = new RelayCommand(Logout);
            ViewSightingCommand = new RelayCommand(ViewSighting, CanViewSighting);

            LoadSightings();
        }

        // ── LOAD DATA ─────────────────────────────────────────
        private void LoadSightings()
        {
            Sightings.Clear();

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    string query = @"
                        SELECT s.SightingId, u.Username, s.Title, s.Description,
                               s.DatePosted, s.Location, s.Province, s.Region,
                               s.Longitude, s.Latitude, s.Status, s.Photo
                        FROM Sighting s
                        INNER JOIN Users u ON s.UserId = u.Id
                        WHERE s.Status = 'Approved'
                        ORDER BY s.DatePosted DESC";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        connection.Open();
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                Sightings.Add(new SightingDisplay
                                {
                                    SightingId = (int)reader["SightingId"],
                                    Username = reader["Username"].ToString()!,
                                    Title = reader["Title"].ToString()!,
                                    Description = reader["Description"].ToString()!,
                                    DatePosted = ((DateTime)reader["DatePosted"]).ToString("MMMM dd, yyyy"),
                                    Location = reader["Location"].ToString()!,
                                    Province = reader["Province"].ToString()!,
                                    Region = reader["Region"].ToString()!,
                                    Longitude = (decimal)reader["Longitude"],
                                    Latitude = (decimal)reader["Latitude"],
                                    Status = reader["Status"].ToString()!,
                                    Photo = reader["Photo"] == DBNull.Value ? string.Empty : reader["Photo"].ToString()!
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to load sightings: " + ex.Message);
            }
        }

        // ── VIEW SIGHTING ─────────────────────────────────────
        private void ViewSighting()
        {
            _navigate(new SightingDetailViewModel(_navigate, _currentUser, _onLogout, SelectedSighting!));
        }

        private bool CanViewSighting() => SelectedSighting != null;

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