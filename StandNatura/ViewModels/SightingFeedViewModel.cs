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

        // ── CONSTRUCTOR ───────────────────────────────────────
        public SightingFeedViewModel(Action<BaseViewModel> navigate, User currentUser)
        {
            _navigate = navigate;
            _currentUser = currentUser;
            GoBackCommand = new RelayCommand(() => _navigate(new AdminHomeViewModel(_navigate, _currentUser)));

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
                               s.Longitude, s.Latitude
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
                                    Latitude = (decimal)reader["Latitude"]
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
    }
}