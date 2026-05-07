using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows;
using StandNatura.Commands;
using StandNatura.Models;
using Microsoft.Data.SqlClient;

namespace StandNatura.ViewModels
{
    public class HiddenSightingsViewModel : BaseViewModel
    {
        private readonly Action<BaseViewModel> _navigate;
        private readonly User _currentUser;
        private readonly Action _onLogout;
        private static readonly string connectionString = DatabaseConfig.ConnectionString;

        // ── BINDABLE PROPERTIES ──────────────────────────────
        private ObservableCollection<SightingDisplay> _hiddenSightings = new();
        public ObservableCollection<SightingDisplay> HiddenSightings
        {
            get => _hiddenSightings;
            set => SetProperty(ref _hiddenSightings, value);
        }

        private SightingDisplay? _selectedSighting;
        public SightingDisplay? SelectedSighting
        {
            get => _selectedSighting;
            set => SetProperty(ref _selectedSighting, value);
        }

        // ── COMMANDS ──────────────────────────────────────────
        public ICommand GoBackCommand { get; }
        public ICommand RestoreCommand { get; }
        public ICommand ArchiveCommand { get; }

        // ── CONSTRUCTOR ───────────────────────────────────────
        public HiddenSightingsViewModel(Action<BaseViewModel> navigate, User currentUser, Action onLogout)
        {
            _navigate = navigate;
            _currentUser = currentUser;
            _onLogout = onLogout;

            GoBackCommand = new RelayCommand(() => _navigate(new AdminHomeViewModel(_navigate, _currentUser, _onLogout)));
            RestoreCommand = new RelayCommand(Restore, CanActOnSighting);
            ArchiveCommand = new RelayCommand(Archive, CanActOnSighting);

            LoadHiddenSightings();
        }

        // ── LOAD DATA ─────────────────────────────────────────
        private void LoadHiddenSightings()
        {
            HiddenSightings.Clear();

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    string query = @"
                        SELECT s.SightingId, u.Username, s.Title, s.Description,
                               s.DatePosted, s.Location, s.Province, s.Region,
                               s.Longitude, s.Latitude, s.Status, s.Photo, s.DenialReason, s.ArchiveReason
                        FROM Sighting s
                        INNER JOIN Users u ON s.UserId = u.Id
                        WHERE s.Status = 'Hidden'
                        ORDER BY s.DatePosted DESC";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        connection.Open();
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                HiddenSightings.Add(new SightingDisplay
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
                                    Photo = reader["Photo"] == DBNull.Value ? string.Empty : reader["Photo"].ToString()!,
                                    DenialReason = reader["DenialReason"] == DBNull.Value ? string.Empty : reader["DenialReason"].ToString()!,
                                    ArchiveReason = reader["ArchiveReason"] == DBNull.Value ? string.Empty : reader["ArchiveReason"].ToString()!
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to load hidden sightings: " + ex.Message);
            }
        }

        // ── RESTORE ───────────────────────────────────────────
        private void Restore()
        {
            var result = MessageBox.Show(
                $"Restore \"{SelectedSighting!.Title}\" to the public feed?",
                "Confirm Restore",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    string query = "UPDATE Sighting SET Status = 'Approved' WHERE SightingId = @id";
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@id", SelectedSighting.SightingId);
                        connection.Open();
                        command.ExecuteNonQuery();
                    }
                }

                MessageBox.Show("Sighting restored to the public feed.", "Success",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                SelectedSighting = null;
                LoadHiddenSightings();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to restore sighting: " + ex.Message);
            }
        }

        // ── ARCHIVE ───────────────────────────────────────────
        private void Archive()
        {
            var result = MessageBox.Show(
                $"Permanently archive \"{SelectedSighting!.Title}\"?\n\n" +
                "This action cannot be undone. The user will be notified.",
                "Confirm Archive",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
                return;

            // Prompt for optional reason
            var reasonDialog = new Views.ArchiveReasonDialog();
            bool? dialogResult = reasonDialog.ShowDialog();

            if (dialogResult != true)
                return;

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    string query = @"UPDATE Sighting 
                                     SET Status = 'Archived', 
                                         ArchiveReason = @reason 
                                     WHERE SightingId = @id";
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@reason",
                            string.IsNullOrWhiteSpace(reasonDialog.Reason) ? DBNull.Value : (object)reasonDialog.Reason);
                        command.Parameters.AddWithValue("@id", SelectedSighting.SightingId);
                        connection.Open();
                        command.ExecuteNonQuery();
                    }
                }

                MessageBox.Show("Sighting archived.", "Success",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                SelectedSighting = null;
                LoadHiddenSightings();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to archive sighting: " + ex.Message);
            }
        }

        // ── CAN EXECUTE ───────────────────────────────────────
        private bool CanActOnSighting() => SelectedSighting != null;
    }
}