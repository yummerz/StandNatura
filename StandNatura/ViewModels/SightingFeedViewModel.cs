using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows;
using StandNatura.Commands;
using StandNatura.Models;
using Microsoft.Data.SqlClient;

namespace StandNatura.ViewModels
{
    public class SightingFeedViewModel : BaseViewModel
    {
        private readonly Action<BaseViewModel> _navigate;
        private readonly User _currentUser;
        private readonly Action _onLogout;
        private static readonly string connectionString = DatabaseConfig.ConnectionString;

        // ── BINDABLE PROPERTIES ──────────────────────────────
        private ObservableCollection<SightingDisplay> _approvedSightings = new();
        public ObservableCollection<SightingDisplay> ApprovedSightings
        {
            get => _approvedSightings;
            set => SetProperty(ref _approvedSightings, value);
        }

        private SightingDisplay? _selectedSighting;
        public SightingDisplay? SelectedSighting
        {
            get => _selectedSighting;
            set => SetProperty(ref _selectedSighting, value);
        }

        // ── COMMANDS ──────────────────────────────────────────
        public ICommand GoBackCommand { get; }
        public ICommand HideForReviewCommand { get; }
        public ICommand ViewDetailsCommand { get; }

        // ── CONSTRUCTOR ───────────────────────────────────────
        public SightingFeedViewModel(Action<BaseViewModel> navigate, User currentUser, Action onLogout)
        {
            _navigate = navigate;
            _currentUser = currentUser;
            _onLogout = onLogout;
            GoBackCommand = new RelayCommand(() => _navigate(new AdminHomeViewModel(_navigate, _currentUser, _onLogout)));
            HideForReviewCommand = new RelayCommand(HideForReview, CanHide);
            ViewDetailsCommand = new RelayCommand(ViewDetails, CanViewDetails);

            LoadApprovedSightings();
        }

        // ── LOAD DATA ─────────────────────────────────────────
        private void LoadApprovedSightings()
        {
            ApprovedSightings.Clear();

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    string query = @"
                SELECT s.SightingId, u.Username, s.Title, s.Description,
                       s.DatePosted, s.Location, s.Province, s.Region,
                       s.Longitude, s.Latitude, s.Photo, s.ArchiveReason
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
                                ApprovedSightings.Add(new SightingDisplay
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
                                    Photo = reader["Photo"] == DBNull.Value ? string.Empty : reader["Photo"].ToString()!,
                                    ArchiveReason = reader["ArchiveReason"] == DBNull.Value ? string.Empty : reader["ArchiveReason"].ToString()!
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
        // ── HIDE FOR REVIEW ───────────────────────────────────
        private void HideForReview()
        {
            var result = MessageBox.Show(
                $"Hide \"{SelectedSighting!.Title}\" for further review?\n\n" +
                "It will be temporarily removed from the public feed while you investigate. " +
                "You can restore it or archive it permanently from the Hidden Sightings page.",
                "Confirm Hide",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    string query = "UPDATE Sighting SET Status = 'Hidden' WHERE SightingId = @id";
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@id", SelectedSighting.SightingId);
                        connection.Open();
                        command.ExecuteNonQuery();
                    }
                }

                MessageBox.Show("Sighting hidden for review.", "Success",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                SelectedSighting = null;
                LoadApprovedSightings();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to hide sighting: " + ex.Message);
            }
        }

        private bool CanHide() => SelectedSighting != null;

        // ── VIEW DETAILS ──────────────────────────────────────
        private void ViewDetails()
        {
            _navigate(new SightingDetailViewModel(_navigate, _currentUser, _onLogout, SelectedSighting!));
        }

        private bool CanViewDetails() => SelectedSighting != null;

    }

}