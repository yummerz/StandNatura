using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows;
using StandNatura.Commands;
using StandNatura.Models;
using Microsoft.Data.SqlClient;

namespace StandNatura.ViewModels
{
    public class CommentDisplay
    {
        public int CommentId { get; set; }
        public string DonorUsername { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string CommentText { get; set; } = string.Empty;

    }

    public class SightingDetailViewModel : BaseViewModel
    {
        private readonly Action<BaseViewModel> _navigate;
        private readonly User _currentUser;
        private readonly Action _onLogout;
        private static readonly string connectionString = DatabaseConfig.ConnectionString;
        // ── ADMIN MODE ────────────────────────────────────────
        public bool IsAdmin => _currentUser.Role == "Admin";
        public bool IsContributor => _currentUser.Role != "Admin";

        // ── SIGHTING INFO ─────────────────────────────────────
        public SightingDisplay Sighting { get; }

        // ── PETITION ──────────────────────────────────────────
        private int _demandCount;
        public int DemandCount
        {
            get => _demandCount;
            set => SetProperty(ref _demandCount, value);
        }

        private bool _hasSigned;
        public bool HasSigned
        {
            get => _hasSigned;
            set => SetProperty(ref _hasSigned, value);
        }

        private string _petitionButtonText = "✊ Sign Petition";
        public string PetitionButtonText
        {
            get => _petitionButtonText;
            set => SetProperty(ref _petitionButtonText, value);
        }

        // ── DONATION ──────────────────────────────────────────
        private string _donationAmount = string.Empty;
        public string DonationAmount
        {
            get => _donationAmount;
            set => SetProperty(ref _donationAmount, value);

        }

        private decimal _totalFunds;
        public decimal TotalFunds
        {
            get => _totalFunds;
            set => SetProperty(ref _totalFunds, value);
        }

        // ── COMMENT ───────────────────────────────────────────
        private string _commentText = string.Empty;
        public string CommentText
        {
            get => _commentText;
            set => SetProperty(ref _commentText, value);
        }

        private bool _canComment;
        public bool CanComment
        {
            get => _canComment;
            set => SetProperty(ref _canComment, value);
        }

        private ObservableCollection<CommentDisplay> _comments = new();
        public ObservableCollection<CommentDisplay> Comments
        {
            get => _comments;
            set => SetProperty(ref _comments, value);
        }

        // ── COMMANDS ──────────────────────────────────────────
        public ICommand GoBackCommand { get; }
        public ICommand SignPetitionCommand { get; }
        public ICommand DonateCommand { get; }
        public ICommand PostCommentCommand { get; }
        public ICommand DeleteCommentCommand { get; }

        // ── CONSTRUCTOR ───────────────────────────────────────
        public SightingDetailViewModel(Action<BaseViewModel> navigate, User currentUser, Action onLogout, SightingDisplay sighting)
        {
            _navigate = navigate;
            _currentUser = currentUser;
            _onLogout = onLogout;
            Sighting = sighting;

            GoBackCommand = new RelayCommand(GoBack);
            SignPetitionCommand = new RelayCommand(SignPetition);
            DonateCommand = new RelayCommand(Donate, CanDonate);
            PostCommentCommand = new RelayCommand(PostComment, CanPostComment);
            DeleteCommentCommand = new RelayCommand<CommentDisplay>(DeleteComment, CanDeleteComment);

            LoadPetition();
            LoadTotalFunds();
            LoadComments();
            CheckIfCanComment();
        }

        // ── LOAD PETITION ─────────────────────────────────────
        private void LoadPetition()
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    // Get or create petition for this sighting
                    string checkQuery = "SELECT PetitionId, DemandCount FROM Petition WHERE SightingId = @sightingId";
                    using (SqlCommand command = new SqlCommand(checkQuery, connection))
                    {
                        command.Parameters.AddWithValue("@sightingId", Sighting.SightingId);
                        connection.Open();
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                int petitionId = (int)reader["PetitionId"];
                                DemandCount = (int)reader["DemandCount"];
                                reader.Close();
                                CheckIfSigned(connection, petitionId);
                            }
                            else
                            {
                                reader.Close();
                                // Create petition if it doesn't exist
                                string insertQuery = "INSERT INTO Petition (SightingId, DateCreated, DemandCount) VALUES (@sightingId, GETDATE(), 0)";
                                using (SqlCommand insertCommand = new SqlCommand(insertQuery, connection))
                                {
                                    insertCommand.Parameters.AddWithValue("@sightingId", Sighting.SightingId);
                                    insertCommand.ExecuteNonQuery();
                                }
                                DemandCount = 0;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to load petition: " + ex.Message);
            }
        }

        // ── CHECK IF SIGNED ───────────────────────────────────
        private void CheckIfSigned(SqlConnection connection, int petitionId)
        {
            string query = "SELECT COUNT(*) FROM PetitionSignature WHERE PetitionId = @petitionId AND UserId = @userId";
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@petitionId", petitionId);
                command.Parameters.AddWithValue("@userId", _currentUser.Id);
                int count = (int)command.ExecuteScalar();
                HasSigned = count > 0;
                PetitionButtonText = HasSigned ? "✊ Unsign Petition" : "✊ Sign Petition";
            }
        }

        // ── SIGN PETITION ─────────────────────────────────────
        private void SignPetition()
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    // Get petition id
                    int petitionId;
                    string getPetitionQuery = "SELECT PetitionId FROM Petition WHERE SightingId = @sightingId";
                    using (SqlCommand command = new SqlCommand(getPetitionQuery, connection))
                    {
                        command.Parameters.AddWithValue("@sightingId", Sighting.SightingId);
                        petitionId = (int)command.ExecuteScalar();
                    }

                    if (HasSigned)
                    {
                        // Unsign
                        string deleteQuery = "DELETE FROM PetitionSignature WHERE PetitionId = @petitionId AND UserId = @userId";
                        using (SqlCommand command = new SqlCommand(deleteQuery, connection))
                        {
                            command.Parameters.AddWithValue("@petitionId", petitionId);
                            command.Parameters.AddWithValue("@userId", _currentUser.Id);
                            command.ExecuteNonQuery();
                        }

                        string updateQuery = "UPDATE Petition SET DemandCount = DemandCount - 1 WHERE PetitionId = @petitionId";
                        using (SqlCommand command = new SqlCommand(updateQuery, connection))
                        {
                            command.Parameters.AddWithValue("@petitionId", petitionId);
                            command.ExecuteNonQuery();
                        }

                        HasSigned = false;
                        DemandCount--;
                        PetitionButtonText = "✊ Sign Petition";
                    }
                    else
                    {
                        // Sign
                        string insertQuery = "INSERT INTO PetitionSignature (PetitionId, UserId) VALUES (@petitionId, @userId)";
                        using (SqlCommand command = new SqlCommand(insertQuery, connection))
                        {
                            command.Parameters.AddWithValue("@petitionId", petitionId);
                            command.Parameters.AddWithValue("@userId", _currentUser.Id);
                            command.ExecuteNonQuery();
                        }

                        string updateQuery = "UPDATE Petition SET DemandCount = DemandCount + 1 WHERE PetitionId = @petitionId";
                        using (SqlCommand command = new SqlCommand(updateQuery, connection))
                        {
                            command.Parameters.AddWithValue("@petitionId", petitionId);
                            command.ExecuteNonQuery();
                        }

                        HasSigned = true;
                        DemandCount++;
                        PetitionButtonText = "✊ Unsign Petition";
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to update petition: " + ex.Message);
            }
        }

        // ── LOAD TOTAL FUNDS ──────────────────────────────────
        private void LoadTotalFunds()
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    string query = "SELECT SUM(Amount) FROM Donation WHERE SightingId = @sightingId";
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@sightingId", Sighting.SightingId);
                        connection.Open();
                        var result = command.ExecuteScalar();
                        TotalFunds = result != DBNull.Value ? (decimal)result : 0;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to load funds: " + ex.Message);
            }
        }

        // ── DONATE ───────────────────────────────────────────
        private void Donate()
        {
            if (!decimal.TryParse(DonationAmount, out decimal amount) || amount <= 0)
            {
                MessageBox.Show("Please enter a valid donation amount.", "Invalid Input",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    string query = "INSERT INTO Donation (UserId, SightingId, Amount) VALUES (@userId, @sightingId, @amount)";
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@userId", _currentUser.Id);
                        command.Parameters.AddWithValue("@sightingId", Sighting.SightingId);
                        command.Parameters.AddWithValue("@amount", amount);
                        connection.Open();
                        command.ExecuteNonQuery();
                    }
                }

                MessageBox.Show($"Thank you for your donation of ₱{amount:N2}! You can now leave a comment.", "Donation Successful",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                DonationAmount = string.Empty;
                LoadTotalFunds();
                CanComment = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to process donation: " + ex.Message);
            }
        }

        private bool CanDonate() =>
            !string.IsNullOrWhiteSpace(DonationAmount) && !CanComment;


        // ── CHECK IF CAN COMMENT ──────────────────────────────
        private void CheckIfCanComment()
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    // User can comment if they have a donation that hasn't been used for a comment yet
                    string query = @"
                SELECT COUNT(*) FROM Donation d
                WHERE d.UserId = @userId 
                  AND d.SightingId = @sightingId
                  AND NOT EXISTS (
                      SELECT 1 FROM Comment c WHERE c.DonationId = d.DonationId
                  )";
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@userId", _currentUser.Id);
                        command.Parameters.AddWithValue("@sightingId", Sighting.SightingId);
                        connection.Open();
                        int count = (int)command.ExecuteScalar();
                        CanComment = count > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to check comment eligibility: " + ex.Message);
            }
        }

        // ── LOAD COMMENTS ─────────────────────────────────────
        private void LoadComments()
        {
            Comments.Clear();

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    string query = @"
                    SELECT c.CommentId, u.Username, d.Amount, c.CommentText
                    FROM Comment c
                    INNER JOIN Donation d ON c.DonationId = d.DonationId
                    INNER JOIN Users u ON d.UserId = u.Id
                    WHERE c.SightingId = @sightingId";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@sightingId", Sighting.SightingId);
                        connection.Open();
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                Comments.Add(new CommentDisplay
                                {
                                    CommentId = (int)reader["CommentId"],
                                    DonorUsername = reader["Username"].ToString()!,
                                    Amount = (decimal)reader["Amount"],
                                    CommentText = reader["CommentText"].ToString()!
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to load comments: " + ex.Message);
            }
        }

        // ── POST COMMENT ──────────────────────────────────────
        private void PostComment()
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    // Get latest donation id for this user and sighting
                    int donationId;
                    string getDonationQuery = @"
                        SELECT TOP 1 d.DonationId FROM Donation d
                        WHERE d.UserId = @userId 
                        AND d.SightingId = @sightingId
                        AND NOT EXISTS (
                        SELECT 1 FROM Comment c WHERE c.DonationId = d.DonationId
                        )
                        ORDER BY d.DonationId DESC";

                    using (SqlCommand command = new SqlCommand(getDonationQuery, connection))
                    {
                        command.Parameters.AddWithValue("@userId", _currentUser.Id);
                        command.Parameters.AddWithValue("@sightingId", Sighting.SightingId);
                        connection.Open();
                        donationId = (int)command.ExecuteScalar();
                    }

                    string insertQuery = "INSERT INTO Comment (SightingId, DonationId, CommentText) VALUES (@sightingId, @donationId, @commentText)";
                    using (SqlCommand command = new SqlCommand(insertQuery, connection))
                    {
                        command.Parameters.AddWithValue("@sightingId", Sighting.SightingId);
                        command.Parameters.AddWithValue("@donationId", donationId);
                        command.Parameters.AddWithValue("@commentText", CommentText.Trim());
                        command.ExecuteNonQuery();
                    }
                }

                CommentText = string.Empty;
                LoadComments();
                CheckIfCanComment(); // Re-evaluate — they just used up their donation
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to post comment: " + ex.Message);
            }
        }
        // ── DELETE COMMENT (Admin only) ───────────────────────
        private void DeleteComment(CommentDisplay comment)
        {
            var result = MessageBox.Show(
                "Are you sure you want to delete this comment?\n\n" +
                "The comment text will be replaced with a moderation notice. " +
                "The donation itself will not be affected.",
                "Confirm Delete Comment",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    string query = "UPDATE Comment SET CommentText = @newText WHERE CommentId = @id";
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@newText", "[Comment removed by moderator]");
                        command.Parameters.AddWithValue("@id", comment.CommentId);
                        connection.Open();
                        command.ExecuteNonQuery();
                    }
                }

                LoadComments();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to delete comment: " + ex.Message);
            }
        }

        private bool CanDeleteComment(CommentDisplay comment) => IsAdmin && comment != null;

        private bool CanPostComment() =>
            CanComment && !string.IsNullOrWhiteSpace(CommentText);

        // ── GO BACK ───────────────────────────────────────────
        private void GoBack()
        {
            if (IsAdmin)
                _navigate(new SightingFeedViewModel(_navigate, _currentUser, _onLogout));
            else
                _navigate(new ContributorHomeViewModel(_navigate, _currentUser, _onLogout));
        }
    }
}