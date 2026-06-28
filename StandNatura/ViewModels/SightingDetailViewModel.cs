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

    public class SightingDetailViewModel : ContributorBaseViewModel
    {
        public override string ActivePageKey => string.Empty;
        private static readonly string connectionString = DatabaseConfig.ConnectionString;
        // ── ADMIN MODE ────────────────────────────────────────
        public bool IsAdmin => _currentUser.Role == "Admin" || _currentUser.Role == "SuperAdmin";
        public bool IsContributor => !IsAdmin;

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
        public bool HasNoComments => Comments == null || Comments.Count == 0;

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
            : base(navigate, currentUser, onLogout)
        {
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

                    // Get petition id for this sighting
                    int petitionId;
                    using (SqlCommand getCmd = new SqlCommand(
                        "SELECT PetitionId FROM Petition WHERE SightingId = @sightingId", connection))
                    {
                        getCmd.Parameters.AddWithValue("@sightingId", Sighting.SightingId);
                        petitionId = (int)getCmd.ExecuteScalar();
                    }

                    // One atomic call; trg_PetitionSignature_SyncDemandCount keeps
                    // Petition.DemandCount correct, so we no longer touch it here.
                    using (SqlCommand command = new SqlCommand("dbo.usp_TogglePetitionSignature", connection))
                    {
                        command.CommandType = System.Data.CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@PetitionId", petitionId);
                        command.Parameters.AddWithValue("@UserId", _currentUser.Id);
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                HasSigned = (bool)reader["HasSigned"];
                                DemandCount = (int)reader["DemandCount"];
                            }
                        }
                    }

                    PetitionButtonText = HasSigned ? "✊ Unsign Petition" : "✊ Sign Petition";
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
                    string query = "SELECT dbo.fn_TotalFundsForSighting(@sightingId)";
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@sightingId", Sighting.SightingId);
                        connection.Open();
                        TotalFunds = (decimal)command.ExecuteScalar();  // UDF ISNULLs to 0
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
                    // Eligibility ("1 donation = 1 comment") is encapsulated in the UDF.
                    string query = "SELECT dbo.fn_CanUserComment(@userId, @sightingId)";
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@userId", _currentUser.Id);
                        command.Parameters.AddWithValue("@sightingId", Sighting.SightingId);
                        connection.Open();
                        CanComment = (bool)command.ExecuteScalar();
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
            OnPropertyChanged(nameof(HasNoComments));
        }

        // ── POST COMMENT ──────────────────────────────────────
        private void PostComment()
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    // Atomic: the proc finds the user's unused donation and inserts the
                    // comment, or raises 50001 if there is no eligible donation.
                    using (SqlCommand command = new SqlCommand("dbo.usp_PostDonationComment", connection))
                    {
                        command.CommandType = System.Data.CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@UserId", _currentUser.Id);
                        command.Parameters.AddWithValue("@SightingId", Sighting.SightingId);
                        command.Parameters.AddWithValue("@CommentText", CommentText.Trim());
                        connection.Open();
                        command.ExecuteNonQuery();
                    }
                }

                CommentText = string.Empty;
                LoadComments();
                CheckIfCanComment(); // Re-evaluate — they just used up their donation
            }
            catch (SqlException ex) when (ex.Number == 50001)
            {
                // Friendly message raised by usp_PostDonationComment (no unused donation).
                MessageBox.Show(ex.Message, "Cannot Comment",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to post comment: " + ex.Message);
            }
            OnPropertyChanged(nameof(HasNoComments));
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
            OnPropertyChanged(nameof(HasNoComments));
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