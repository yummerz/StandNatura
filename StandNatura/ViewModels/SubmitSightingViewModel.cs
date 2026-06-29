using System;
using System.Windows.Input;
using System.Windows;
using StandNatura.Commands;
using StandNatura.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Win32;
using System.IO;

namespace StandNatura.ViewModels
{
    public class SubmitSightingViewModel : ContributorBaseViewModel
    {
        private static readonly string connectionString = DatabaseConfig.ConnectionString;
        private readonly int? _editingSightingId;

        // Photos are copied here (relative to the app folder) so the stored
        // path is portable across machines instead of an absolute C:\... path.
        private const string PhotoFolderRelative = @"Assets\SightingPhotos";

        // Max NEW sightings a user may create per calendar day (anti-spam).
        private const int DailySubmissionLimit = 3;

        // ── ACTIVE PAGE KEY ───────────────────────────────────
        public override string ActivePageKey => "Submit";

        // ── BINDABLE PROPERTIES ──────────────────────────────
        private string _title = string.Empty;
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        private string _description = string.Empty;
        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        private string _location = string.Empty;
        public string Location
        {
            get => _location;
            set => SetProperty(ref _location, value);
        }

        private string _province = string.Empty;
        public string Province
        {
            get => _province;
            set => SetProperty(ref _province, value);
        }

        private string _region = string.Empty;
        public string Region
        {
            get => _region;
            set => SetProperty(ref _region, value);
        }

        private string _longitude = string.Empty;
        public string Longitude
        {
            get => _longitude;
            set => SetProperty(ref _longitude, value);
        }

        private string _latitude = string.Empty;
        public string Latitude
        {
            get => _latitude;
            set => SetProperty(ref _latitude, value);
        }

        private string _photoPath = string.Empty;
        public string PhotoPath
        {
            get => _photoPath;
            set => SetProperty(ref _photoPath, value);
        }

        // ── DYNAMIC TEXT BASED ON MODE ────────────────────────
        public string SubmitButtonText => _editingSightingId.HasValue
            ? "📤 Resubmit Sighting"
            : "📍 Submit Sighting";

        public string HeaderText => _editingSightingId.HasValue
            ? "Edit & Resubmit"
            : "Submit a Sighting";

        public string SubHeaderText => _editingSightingId.HasValue
            ? "Make any edits and resubmit for admin review."
            : "Help us protect what you've discovered. Submissions are reviewed by an admin before going live in the community feed.";

        // ── COMMANDS ──────────────────────────────────────────
        public ICommand CancelCommand { get; }
        public ICommand BrowsePhotoCommand { get; }
        public ICommand SubmitSightingCommand { get; }

        // ── CONSTRUCTOR (new submission) ──────────────────────
        public SubmitSightingViewModel(Action<BaseViewModel> navigate, User currentUser, Action onLogout)
            : base(navigate, currentUser, onLogout)
        {
            CancelCommand = new RelayCommand(() => _navigate(new ContributorHomeViewModel(_navigate, _currentUser, _onLogout)));
            BrowsePhotoCommand = new RelayCommand(BrowsePhoto);
            SubmitSightingCommand = new RelayCommand(SubmitSighting, CanSubmit);
        }

        // ── CONSTRUCTOR (edit/resubmit existing sighting) ─────
        public SubmitSightingViewModel(Action<BaseViewModel> navigate, User currentUser, Action onLogout, SightingDisplay existingSighting)
            : this(navigate, currentUser, onLogout)
        {
            _editingSightingId = existingSighting.SightingId;

            // Pre-fill the form
            Title = existingSighting.Title;
            Description = existingSighting.Description;
            Location = existingSighting.Location;
            Province = existingSighting.Province;
            Region = existingSighting.Region;
            Longitude = existingSighting.Longitude.ToString();
            Latitude = existingSighting.Latitude.ToString();
            PhotoPath = existingSighting.Photo;
        }

        // ── BROWSE PHOTO ──────────────────────────────────────
        private void BrowsePhoto()
        {
            OpenFileDialog dialog = new OpenFileDialog
            {
                Title = "Select a Photo",
                Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp"
            };

            if (dialog.ShowDialog() == true)
            {
                PhotoPath = dialog.FileName;
            }
        }

        // ── SUBMIT SIGHTING ───────────────────────────────────
        private void SubmitSighting()
        {
            if (!decimal.TryParse(Longitude, out decimal longitude))
            {
                MessageBox.Show("Please enter a valid Longitude value.", "Invalid Input",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (longitude < -180 || longitude > 180)
            {
                MessageBox.Show("Longitude must be between -180 and 180.\n\nExample for Philippines: 121.0244", "Invalid Longitude",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!decimal.TryParse(Latitude, out decimal latitude))
            {
                MessageBox.Show("Please enter a valid Latitude value.", "Invalid Input",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (latitude < -90 || latitude > 90)
            {
                MessageBox.Show("Latitude must be between -90 and 90.\n\nExample for Philippines: 14.5995", "Invalid Latitude",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    // Daily cap: a user may create at most DailySubmissionLimit NEW sightings
                    // per calendar day. Resubmissions (edits to an existing row) are exempt and
                    // never blocked — but their bumped DatePosted still counts toward the total.
                    if (!_editingSightingId.HasValue)
                    {
                        using (SqlCommand countCmd = new SqlCommand(
                            "SELECT dbo.fn_GetUserSubmissionCountToday(@userId)", connection))
                        {
                            countCmd.Parameters.AddWithValue("@userId", _currentUser.Id);
                            int todayCount = (int)countCmd.ExecuteScalar();
                            if (todayCount >= DailySubmissionLimit)
                            {
                                MessageBox.Show(
                                    $"You've reached today's limit of {DailySubmissionLimit} sightings. " +
                                    "You can submit again tomorrow.",
                                    "Daily Limit Reached",
                                    MessageBoxButton.OK, MessageBoxImage.Warning);
                                return;
                            }
                        }
                    }

                    string query;

                    if (_editingSightingId.HasValue)
                    {
                        query = @"UPDATE Sighting SET 
                            Title = @title,
                            Description = @description,
                            Photo = @photo,
                            Location = @location,
                            Province = @province,
                            Region = @region,
                            Longitude = @longitude,
                            Latitude = @latitude,
                            Status = 'Pending',
                            DenialReason = NULL,
                            ArchiveReason = NULL,
                            DatePosted = GETDATE()
                          WHERE SightingId = @sightingId";
                    }
                    else
                    {
                        query = @"INSERT INTO Sighting 
                            (UserId, Title, Description, DatePosted, Photo, Location, Province, Region, Longitude, Latitude, Status)
                            VALUES 
                            (@userId, @title, @description, GETDATE(), @photo, @location, @province, @region, @longitude, @latitude, 'Pending')";
                    }

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        if (_editingSightingId.HasValue)
                            command.Parameters.AddWithValue("@sightingId", _editingSightingId.Value);
                        else
                            command.Parameters.AddWithValue("@userId", _currentUser.Id);

                        command.Parameters.AddWithValue("@title", Title.Trim());
                        command.Parameters.AddWithValue("@description", Description.Trim());
                        string? storedPhoto = ResolvePhotoForStorage();
                        command.Parameters.AddWithValue("@photo", storedPhoto is null ? DBNull.Value : (object)storedPhoto);
                        command.Parameters.AddWithValue("@location", Location.Trim());
                        command.Parameters.AddWithValue("@province", Province.Trim());
                        command.Parameters.AddWithValue("@region", Region.Trim());
                        command.Parameters.AddWithValue("@longitude", longitude);
                        command.Parameters.AddWithValue("@latitude", latitude);
                        command.ExecuteNonQuery();
                    }
                }

                string successMessage = _editingSightingId.HasValue
                    ? "Sighting resubmitted successfully! It will be reviewed by an admin."
                    : "Sighting submitted successfully! It will be reviewed by an admin.";

                MessageBox.Show(successMessage, "Success",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                if (_editingSightingId.HasValue)
                    _navigate(new MySightingsViewModel(_navigate, _currentUser, _onLogout));
                else
                    ClearForm();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to submit sighting: " + ex.Message);
            }
        }

        // ── PHOTO STORAGE ─────────────────────────────────────
        // Decides what to store in the DB Photo column.
        private string? ResolvePhotoForStorage()
        {
            if (string.IsNullOrWhiteSpace(PhotoPath))
                return null;

            // Already an app-relative path (resubmitting without changing photo).
            if (!Path.IsPathRooted(PhotoPath))
                return PhotoPath;

            // A freshly-picked absolute path → copy it into the app folder.
            if (File.Exists(PhotoPath))
                return CopyPhotoIntoAppFolder(PhotoPath);

            // Absolute path that no longer exists (e.g. old data) → leave as-is.
            return PhotoPath;
        }

        private static string CopyPhotoIntoAppFolder(string sourcePath)
        {
            string targetDir = Path.Combine(AppContext.BaseDirectory, PhotoFolderRelative);
            Directory.CreateDirectory(targetDir);

            string fileName = $"{Guid.NewGuid():N}{Path.GetExtension(sourcePath)}";
            File.Copy(sourcePath, Path.Combine(targetDir, fileName), overwrite: false);

            // Store a RELATIVE path so it resolves on any machine.
            return Path.Combine(PhotoFolderRelative, fileName);
        }

        // ── CLEAR FORM ────────────────────────────────────────
        private void ClearForm()
        {
            Title = string.Empty;
            Description = string.Empty;
            Location = string.Empty;
            Province = string.Empty;
            Region = string.Empty;
            Longitude = string.Empty;
            Latitude = string.Empty;
            PhotoPath = string.Empty;
        }

        // ── CAN EXECUTE ───────────────────────────────────────
        private bool CanSubmit() =>
            !string.IsNullOrWhiteSpace(Title) &&
            !string.IsNullOrWhiteSpace(Description) &&
            !string.IsNullOrWhiteSpace(Location) &&
            !string.IsNullOrWhiteSpace(Province) &&
            !string.IsNullOrWhiteSpace(Region) &&
            !string.IsNullOrWhiteSpace(Longitude) &&
            !string.IsNullOrWhiteSpace(Latitude) &&
            !string.IsNullOrWhiteSpace(PhotoPath);
    }
}