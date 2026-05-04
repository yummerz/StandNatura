using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows;
using StandNatura.Commands;
using StandNatura.Models;
using Microsoft.Data.SqlClient;

namespace StandNatura.ViewModels
{
    public class VerifyPostsViewModel : BaseViewModel
    {
        private readonly Action<BaseViewModel> _navigate;
        private readonly User _currentUser;
        private static readonly string connectionString = DatabaseConfig.ConnectionString;

        // ── BINDABLE PROPERTIES ──────────────────────────────
        private ObservableCollection<Sighting> _pendingSightings = new();
        public ObservableCollection<Sighting> PendingSightings
        {
            get => _pendingSightings;
            set => SetProperty(ref _pendingSightings, value);
        }

        private Sighting? _selectedSighting;
        public Sighting? SelectedSighting
        {
            get => _selectedSighting;
            set => SetProperty(ref _selectedSighting, value);
        }

        // ── COMMANDS ──────────────────────────────────────────
        public ICommand GoBackCommand { get; }
        public ICommand ApproveCommand { get; }
        public ICommand DenyCommand { get; }

        // ── CONSTRUCTOR ───────────────────────────────────────
        public VerifyPostsViewModel(Action<BaseViewModel> navigate, User currentUser)
        {
            _navigate = navigate;
            _currentUser = currentUser;

            GoBackCommand = new RelayCommand(() => _navigate(new AdminHomeViewModel(_navigate, _currentUser)));
            ApproveCommand = new RelayCommand(ApprovePost, CanActOnPost);
            DenyCommand = new RelayCommand(DenyPost, CanActOnPost);

            LoadPendingSightings();
        }

        // ── LOAD DATA ─────────────────────────────────────────
        private void LoadPendingSightings()
        {
            PendingSightings.Clear();

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    string query = "SELECT * FROM Sighting WHERE Status = 'Pending'";
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        connection.Open();
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                PendingSightings.Add(new Sighting
                                {
                                    SightingId = (int)reader["SightingId"],
                                    UserId = (int)reader["UserId"],
                                    Title = reader["Title"].ToString()!,
                                    Description = reader["Description"].ToString()!,
                                    DatePosted = (DateTime)reader["DatePosted"],
                                    Location = reader["Location"].ToString()!,
                                    Province = reader["Province"].ToString()!,
                                    Region = reader["Region"].ToString()!,
                                    Longitude = (decimal)reader["Longitude"],
                                    Latitude = (decimal)reader["Latitude"],
                                    Status = reader["Status"].ToString()!
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

        // ── APPROVE ───────────────────────────────────────────
        private void ApprovePost()
        {
            var result = MessageBox.Show(
                $"Are you sure you want to APPROVE\n\"{SelectedSighting!.Title}\"?\n\nThis action cannot be undone.",
                "Confirm Approval",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
                UpdateStatus(SelectedSighting.SightingId, "Approved");
        }

        // ── DENY ──────────────────────────────────────────────
        private void DenyPost()
        {
            var result = MessageBox.Show(
                $"Are you sure you want to DENY\n\"{SelectedSighting!.Title}\"?\n\nThis action cannot be undone.",
                "Confirm Denial",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
                UpdateStatus(SelectedSighting.SightingId, "Denied");
        }

        // ── UPDATE STATUS ─────────────────────────────────────
        private void UpdateStatus(int sightingId, string status)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    string query = "UPDATE Sighting SET Status = @status WHERE SightingId = @id";
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@status", status);
                        command.Parameters.AddWithValue("@id", sightingId);
                        connection.Open();
                        command.ExecuteNonQuery();
                    }
                }

                MessageBox.Show($"Sighting has been {status}.", "Success",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                LoadPendingSightings();
                SelectedSighting = null;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to update sighting: " + ex.Message);
            }
        }

        // ── CAN EXECUTE ───────────────────────────────────────
        private bool CanActOnPost() => SelectedSighting != null;
    }
}