using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows;
using StandNatura.Commands;
using StandNatura.Models;
using Microsoft.Data.SqlClient;

namespace StandNatura.ViewModels
{
    public class DonationDisplay
    {
        public int DonationId { get; set; }
        public string DonorUsername { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string SightingTitle { get; set; } = string.Empty;
        public int SightingId { get; set; }
    }

    public class DonationsFundsViewModel : BaseViewModel
    {
        private readonly Action<BaseViewModel> _navigate;
        private readonly User _currentUser;
        private readonly Action _onLogout;
        private static readonly string connectionString = DatabaseConfig.ConnectionString;

        // ── BINDABLE PROPERTIES ──────────────────────────────
        private ObservableCollection<DonationDisplay> _donations = new();
        public ObservableCollection<DonationDisplay> Donations
        {
            get => _donations;
            set => SetProperty(ref _donations, value);
        }

        private DonationDisplay? _selectedDonation;
        public DonationDisplay? SelectedDonation
        {
            get => _selectedDonation;
            set
            {
                SetProperty(ref _selectedDonation, value);
                if (value != null)
                    LoadTotalFunds(value.SightingId);
            }
        }

        private decimal _totalFunds;
        public decimal TotalFunds
        {
            get => _totalFunds;
            set => SetProperty(ref _totalFunds, value);
        }

        // ── COMMANDS ──────────────────────────────────────────
        public ICommand GoBackCommand { get; }
        public ICommand DeleteDonationCommand { get; }

        // ── CONSTRUCTOR ───────────────────────────────────────
        public DonationsFundsViewModel(Action<BaseViewModel> navigate, User currentUser, Action onLogout)
        {
            _navigate = navigate;
            _currentUser = currentUser;
            _onLogout = onLogout;

            GoBackCommand = new RelayCommand(() => _navigate(new AdminHomeViewModel(_navigate, _currentUser, _onLogout)));
            DeleteDonationCommand = new RelayCommand(DeleteDonation, CanDeleteDonation);

            LoadDonations();
        }

        // ── LOAD DONATIONS ────────────────────────────────────
        private void LoadDonations()
        {
            Donations.Clear();

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    string query = @"
                        SELECT d.DonationId, u.Username AS DonorUsername,
                               d.Amount, s.Title AS SightingTitle, s.SightingId
                        FROM Donation d
                        INNER JOIN Users u ON d.UserId = u.Id
                        INNER JOIN Sighting s ON d.SightingId = s.SightingId
                        ORDER BY d.DonationId DESC";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        connection.Open();
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                Donations.Add(new DonationDisplay
                                {
                                    DonationId = (int)reader["DonationId"],
                                    DonorUsername = reader["DonorUsername"].ToString()!,
                                    Amount = (decimal)reader["Amount"],
                                    SightingTitle = reader["SightingTitle"].ToString()!,
                                    SightingId = (int)reader["SightingId"]
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to load donations: " + ex.Message);
            }
        }

        // ── LOAD TOTAL FUNDS PER SIGHTING ─────────────────────
        private void LoadTotalFunds(int sightingId)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    string query = "SELECT SUM(Amount) FROM Donation WHERE SightingId = @sightingId";
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@sightingId", sightingId);
                        connection.Open();
                        var result = command.ExecuteScalar();
                        TotalFunds = result != DBNull.Value ? (decimal)result : 0;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to load total funds: " + ex.Message);
            }
        }

        // ── DELETE DONATION ───────────────────────────────────
        private void DeleteDonation()
        {
            var result = MessageBox.Show(
                $"Are you sure you want to delete this donation from '{SelectedDonation!.DonorUsername}'?\n\nAmount: ₱{SelectedDonation.Amount:N2}\nThis action cannot be undone.",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    using (SqlConnection connection = new SqlConnection(connectionString))
                    {
                        string query = "DELETE FROM Donation WHERE DonationId = @id";
                        using (SqlCommand command = new SqlCommand(query, connection))
                        {
                            command.Parameters.AddWithValue("@id", SelectedDonation.DonationId);
                            connection.Open();
                            command.ExecuteNonQuery();
                        }
                    }

                    MessageBox.Show("Donation has been deleted.", "Success",
                        MessageBoxButton.OK, MessageBoxImage.Information);

                    SelectedDonation = null;
                    TotalFunds = 0;
                    LoadDonations();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Failed to delete donation: " + ex.Message);
                }
            }
        }

        // ── CAN EXECUTE ───────────────────────────────────────
        private bool CanDeleteDonation() => SelectedDonation != null;
    }
}