using System;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Data.SqlClient;
using StandNatura.Models;

namespace StandNatura.Services
{
    /// <summary>
    /// Shared "Sign in with Google" flow used by BOTH the Login and Register
    /// screens: runs the OAuth flow, finds-or-creates the matching Users row,
    /// shows a one-time welcome for brand-new accounts, then signs the user in.
    /// Each ViewModel exposes a thin command that just calls SignInAsync.
    /// </summary>
    public static class GoogleSignInService
    {
        private static readonly string connectionString = DatabaseConfig.ConnectionString;

        public static async Task SignInAsync(Action<User> onLoginSuccess)
        {
            GoogleUser? googleUser;
            try
            {
                googleUser = await GoogleAuthService.SignInAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Google sign-in failed: " + ex.Message, "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // User closed the browser / didn't complete consent.
            if (googleUser == null || string.IsNullOrWhiteSpace(googleUser.Email))
                return;

            try
            {
                var (user, isNew) = FindOrCreateGoogleUser(googleUser.Email.Trim());

                // Only first-time Google sign-ins (account just created) see this.
                if (isNew)
                    MessageBox.Show(
                        "Welcome to StandNatura! We've created a contributor account for you " +
                        "so you can start sharing sightings.",
                        "Account Created", MessageBoxButton.OK, MessageBoxImage.Information);

                onLoginSuccess(user);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not complete Google sign-in: " + ex.Message, "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Looks up an existing account by email (so a repeat sign-in does NOT
        // create a duplicate), otherwise auto-creates a Contributor. The password
        // is an unguessable placeholder (so password login is impossible) and the
        // "google:" prefix marks the row as a Google-created account.
        private static (User user, bool isNew) FindOrCreateGoogleUser(string email)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                using (SqlCommand select = new SqlCommand(
                    "SELECT Id, Username, Role FROM Users WHERE Username = @username", connection))
                {
                    select.Parameters.AddWithValue("@username", email);
                    using (SqlDataReader reader = select.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            var existing = new User
                            {
                                Id = (int)reader["Id"],
                                Username = reader["Username"].ToString()!,
                                Role = reader["Role"].ToString()!
                            };
                            return (existing, false);
                        }
                    }
                }

                // Google accounts have no real password — hash a random value so
                // password login is impossible while satisfying the NOT NULL columns.
                var (hash, salt) = PasswordHasher.HashPassword(Guid.NewGuid().ToString("N"));
                using (SqlCommand insert = new SqlCommand(
                    "INSERT INTO Users (Username, PasswordHash, Salt, Role) OUTPUT INSERTED.Id " +
                    "VALUES (@username, @hash, @salt, 'Contributor')", connection))
                {
                    insert.Parameters.AddWithValue("@username", email);
                    insert.Parameters.AddWithValue("@hash", hash);
                    insert.Parameters.AddWithValue("@salt", salt);
                    int newId = (int)insert.ExecuteScalar();
                    var created = new User { Id = newId, Username = email, Role = "Contributor" };
                    return (created, true);
                }
            }
        }
    }
}
