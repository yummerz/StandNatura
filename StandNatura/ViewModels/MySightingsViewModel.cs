using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows;
using StandNatura.Commands;
using StandNatura.Models;
using Microsoft.Data.SqlClient;

namespace StandNatura.ViewModels
{
    public class MySightingsViewModel : BaseViewModel
    {
        private readonly Action<BaseViewModel> _navigate;
        private readonly User _currentUser;
        private readonly Action _onLogout;
        private static readonly string connectionString = DatabaseConfig.ConnectionString;

        // ── BINDABLE PROPERTIES ──────────────────────────────
        private ObservableCollection<SightingDisplay> _mySightings = new();
        public ObservableCollection<SightingDisplay> MySightings
        {
            get => _mySightings;
            set => SetProperty(ref _mySightings, value);
        }

        private SightingDisplay? _selectedSighting;
        public SightingDisplay? SelectedSighting
        {
            get => _selectedSighting;
            set => SetProperty(ref _selectedSighting, value);
        }

        // ── COMMANDS ──────────────────────────────────────────
        public ICommand GoBackCommand { get; }
        public ICommand DeleteSightingCommand { get; }

        // ── CONSTRUCTOR ───────────────────────────────────────
        public MySightingsViewModel(Action<BaseViewModel> navigate, User currentUser, Action onLogout)
        {
            _navigate = navigate;
            _currentUser = currentUser;
            _onLogout = onLogout;

            GoBackCommand = new RelayCommand(() => _navigate(new ContributorHomeViewModel(_navigate, _currentUser, _onLogout)));
            DeleteSightingCommand = new RelayCommand(DeleteSighting, CanDelete);

            LoadMySightings();
        }

        // ── LOAD DATA ─────────────────────────────────────────
        private void LoadMySightings()
        {
            MySightings.Clear();

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
                        WHERE s.UserId = @userId
                        ORDER BY s.DatePosted DESC";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@userId", _currentUser.Id);
                        connection.Open();
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                MySightings.Add(new SightingDisplay
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

        // ── DELETE SIGHTING ───────────────────────────────────
        private void DeleteSighting()
        {
            if (SelectedSighting!.Status == "Approved")
            {
                MessageBox.Show("Approved sightings cannot be deleted.", "Action Blocked",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show(
                $"Are you sure you want to delete\n\"{SelectedSighting.Title}\"?\n\nThis action cannot be undone.",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    using (SqlConnection connection = new SqlConnection(connectionString))
                    {
                        string query = "DELETE FROM Sighting WHERE SightingId = @id";
                        using (SqlCommand command = new SqlCommand(query, connection))
                        {
                            command.Parameters.AddWithValue("@id", SelectedSighting.SightingId);
                            connection.Open();
                            command.ExecuteNonQuery();
                        }
                    }

                    MessageBox.Show("Sighting deleted successfully.", "Success",
                        MessageBoxButton.OK, MessageBoxImage.Information);

                    SelectedSighting = null;
                    LoadMySightings();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Failed to delete sighting: " + ex.Message);
                }
            }
        }

        // ── CAN EXECUTE ───────────────────────────────────────
        private bool CanDelete() => SelectedSighting != null;
    }
}