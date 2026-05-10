using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows;
using StandNatura.Commands;
using StandNatura.Models;
using Microsoft.Data.SqlClient;

namespace StandNatura.ViewModels
{
    public class MySightingsViewModel : ContributorBaseViewModel
    {
        private static readonly string connectionString = DatabaseConfig.ConnectionString;

        // ── ACTIVE PAGE KEY ───────────────────────────────────
        public override string ActivePageKey => "MySightings";

        // ── GROUPED COLLECTIONS ───────────────────────────────
        // One collection per status section so the view can render
        // each as its own ItemsControl with its own visibility logic.
        public ObservableCollection<SightingDisplay> NeedsAttentionSightings { get; } = new();
        public ObservableCollection<SightingDisplay> AwaitingReviewSightings { get; } = new();
        public ObservableCollection<SightingDisplay> LiveInFeedSightings { get; } = new();
        public ObservableCollection<SightingDisplay> ArchivedSightings { get; } = new();

        // ── SECTION VISIBILITY HELPERS ────────────────────────
        // Bound to each section's Visibility so empty sections hide.
        public bool HasNeedsAttention => NeedsAttentionSightings.Count > 0;
        public bool HasAwaitingReview => AwaitingReviewSightings.Count > 0;
        public bool HasLiveInFeed => LiveInFeedSightings.Count > 0;
        public bool HasArchived => ArchivedSightings.Count > 0;

        // Empty state shows when ALL sections are empty.
        public bool IsEmpty =>
            !HasNeedsAttention &&
            !HasAwaitingReview &&
            !HasLiveInFeed &&
            !HasArchived;

        // ── SECTION COUNT LABELS ──────────────────────────────
        // Used in section headers like "NEEDS YOUR ATTENTION (2)"
        public int NeedsAttentionCount => NeedsAttentionSightings.Count;
        public int AwaitingReviewCount => AwaitingReviewSightings.Count;
        public int LiveInFeedCount => LiveInFeedSightings.Count;
        public int ArchivedCount => ArchivedSightings.Count;

        // ── COMMANDS (per-card, take SightingDisplay parameter) ──
        public ICommand EditAndResubmitCommand { get; }
        public ICommand QuickResubmitCommand { get; }
        public ICommand DeleteSightingCommand { get; }
        public ICommand ViewInFeedCommand { get; }

        // ── CONSTRUCTOR ───────────────────────────────────────
        public MySightingsViewModel(Action<BaseViewModel> navigate, User currentUser, Action onLogout)
            : base(navigate, currentUser, onLogout)
        {
            EditAndResubmitCommand = new RelayCommand<SightingDisplay>(EditAndResubmit);
            QuickResubmitCommand = new RelayCommand<SightingDisplay>(QuickResubmit);
            DeleteSightingCommand = new RelayCommand<SightingDisplay>(DeleteSighting);
            ViewInFeedCommand = new RelayCommand<SightingDisplay>(ViewInFeed);

            LoadMySightings();
        }

        // ── LOAD DATA ─────────────────────────────────────────
        private void LoadMySightings()
        {
            // Clear all sections before reloading
            NeedsAttentionSightings.Clear();
            AwaitingReviewSightings.Clear();
            LiveInFeedSightings.Clear();
            ArchivedSightings.Clear();

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    string query = @"
                        SELECT s.SightingId, u.Username, s.Title, s.Description,
                               s.DatePosted, s.Location, s.Province, s.Region,
                               s.Longitude, s.Latitude, s.Status, s.Photo,
                               s.DenialReason, s.ArchiveReason
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
                                var sighting = new SightingDisplay
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
                                };

                                // Route into the right section based on status
                                switch (sighting.Status)
                                {
                                    case "Denied":
                                        NeedsAttentionSightings.Add(sighting);
                                        break;
                                    case "Pending":
                                        AwaitingReviewSightings.Add(sighting);
                                        break;
                                    case "Approved":
                                    case "Hidden":
                                        LiveInFeedSightings.Add(sighting);
                                        break;
                                    case "Archived":
                                        ArchivedSightings.Add(sighting);
                                        break;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to load sightings: " + ex.Message);
            }

            // Notify the UI that all the visibility/count properties may have changed
            OnPropertyChanged(nameof(HasNeedsAttention));
            OnPropertyChanged(nameof(HasAwaitingReview));
            OnPropertyChanged(nameof(HasLiveInFeed));
            OnPropertyChanged(nameof(HasArchived));
            OnPropertyChanged(nameof(IsEmpty));
            OnPropertyChanged(nameof(NeedsAttentionCount));
            OnPropertyChanged(nameof(AwaitingReviewCount));
            OnPropertyChanged(nameof(LiveInFeedCount));
            OnPropertyChanged(nameof(ArchivedCount));
        }

        // ── EDIT AND RESUBMIT (Denied or Archived) ────────────
        private void EditAndResubmit(SightingDisplay sighting)
        {
            if (sighting == null) return;
            _navigate(new SubmitSightingViewModel(_navigate, _currentUser, _onLogout, sighting));
        }

        // ── QUICK RESUBMIT (Denied only, no edits) ────────────
        private void QuickResubmit(SightingDisplay sighting)
        {
            if (sighting == null) return;

            var result = MessageBox.Show(
                $"Resubmit \"{sighting.Title}\" without changes?\n\nIt will return to Pending status for admin review.",
                "Confirm Resubmit",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    string query = @"UPDATE Sighting 
                                     SET Status = 'Pending', 
                                         DenialReason = NULL,
                                         DatePosted = GETDATE()
                                     WHERE SightingId = @id";
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@id", sighting.SightingId);
                        connection.Open();
                        command.ExecuteNonQuery();
                    }
                }

                MessageBox.Show("Sighting resubmitted! It is now pending admin review.", "Success",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                LoadMySightings();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to resubmit sighting: " + ex.Message);
            }
        }

        // ── DELETE SIGHTING (Pending or Denied only) ──────────
        private void DeleteSighting(SightingDisplay sighting)
        {
            if (sighting == null) return;

            if (sighting.Status == "Approved" || sighting.Status == "Archived" || sighting.Status == "Hidden")
            {
                MessageBox.Show($"{sighting.Status} sightings cannot be deleted.", "Action Blocked",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show(
                $"Are you sure you want to delete\n\"{sighting.Title}\"?\n\nThis action cannot be undone.",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    string query = "DELETE FROM Sighting WHERE SightingId = @id";
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@id", sighting.SightingId);
                        connection.Open();
                        command.ExecuteNonQuery();
                    }
                }

                MessageBox.Show("Sighting deleted successfully.", "Success",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                LoadMySightings();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to delete sighting: " + ex.Message);
            }
        }

        // ── VIEW IN FEED (opens SightingDetailView) ───────────
        private void ViewInFeed(SightingDisplay sighting)
        {
            if (sighting == null) return;
            _navigate(new SightingDetailViewModel(_navigate, _currentUser, _onLogout, sighting));
        }

        // ── GO TO SUBMIT (used by empty state button) ─────────
        // Reuses the inherited GoToSubmitSightingCommand from the base.
    }
}