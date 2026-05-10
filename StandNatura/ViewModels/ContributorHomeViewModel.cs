using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows;
using StandNatura.Commands;
using StandNatura.Models;
using Microsoft.Data.SqlClient;

namespace StandNatura.ViewModels
{
    public class ContributorHomeViewModel : ContributorBaseViewModel
    {
        private static readonly string connectionString = DatabaseConfig.ConnectionString;

        // ── ACTIVE PAGE KEY ───────────────────────────────────
        public override string ActivePageKey => "Home";

        // ── BINDABLE PROPERTIES ──────────────────────────────
        private ObservableCollection<SightingDisplay> _sightings = new();
        public ObservableCollection<SightingDisplay> Sightings
        {
            get => _sightings;
            set => SetProperty(ref _sightings, value);
        }

        // ── COMMANDS ──────────────────────────────────────────
        public ICommand ViewSightingCommand { get; }

        // ── CONSTRUCTOR ───────────────────────────────────────
        public ContributorHomeViewModel(Action<BaseViewModel> navigate, User currentUser, Action onLogout)
            : base(navigate, currentUser, onLogout)
        {
            ViewSightingCommand = new RelayCommand<SightingDisplay>(ViewSighting);
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
        private void ViewSighting(SightingDisplay sighting)
        {
            if (sighting == null) return;
            _navigate(new SightingDetailViewModel(_navigate, _currentUser, _onLogout, sighting));
        }
    }
}