using System;
using StandNatura.Commands;
using StandNatura.Models;
using System.Windows.Input;
using System.Windows;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;

namespace StandNatura.ViewModels
{
    public class HotspotMapViewModel : ContributorBaseViewModel
    {
        // ── ACTIVE PAGE KEY ───────────────────────────────────
        public override string ActivePageKey => "HotspotMap";
        private static readonly string connectionString = DatabaseConfig.ConnectionString;

        // JSON array of approved-sighting pins, consumed by the map HTML.
        public string PinsJson { get; private set; } = "[]";

        // ── COMMANDS ──────────────────────────────────────────
        public ICommand GoBackCommand { get; }

        // ── CONSTRUCTOR ───────────────────────────────────────
        public HotspotMapViewModel(Action<BaseViewModel> navigate, User currentUser, Action onLogout)
            : base(navigate, currentUser, onLogout)
        {
            GoBackCommand = new RelayCommand(() => _navigate(new ContributorHomeViewModel(_navigate, _currentUser, _onLogout)));
            LoadPins();
        }

        // ── LOAD APPROVED SIGHTINGS AS MAP PINS ───────────────
        private void LoadPins()
        {
            var pins = new List<object>();
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    string query = @"SELECT Title, Location, Latitude, Longitude
                                     FROM Sighting WHERE Status = 'Approved'";
                    using (var command = new SqlCommand(query, connection))
                    {
                        connection.Open();
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                pins.Add(new
                                {
                                    title = reader["Title"].ToString(),
                                    location = reader["Location"].ToString(),
                                    lat = (double)(decimal)reader["Latitude"],
                                    lng = (double)(decimal)reader["Longitude"]
                                });
                            }
                        }
                    }
                }
                PinsJson = System.Text.Json.JsonSerializer.Serialize(pins);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to load map pins: " + ex.Message);
            }
        }
    }
}