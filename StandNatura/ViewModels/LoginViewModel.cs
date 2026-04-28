using System;
using StandNatura.Commands;
using StandNatura.Models;
using System.Windows.Input;
using Microsoft.Data.SqlClient;
using System.Windows;

namespace StandNatura.ViewModels
{
    public class LoginViewModel : BaseViewModel
    {
        private readonly Action<User> _onLoginSuccess;
        private static readonly string connectionString = DatabaseConfig.ConnectionString;

        // ── BINDABLE PROPERTIES ──────────────────────────────
        private string _username = string.Empty;
        public string Username
        {
            get => _username;
            set => SetProperty(ref _username, value);
        }

        private string _password = string.Empty;
        public string Password
        {
            get => _password;
            set => SetProperty(ref _password, value);
        }

        // ── COMMANDS ──────────────────────────────────────────
        public ICommand LoginCommand { get; }

        // ── CONSTRUCTOR ───────────────────────────────────────
        public LoginViewModel(Action<User> onLoginSuccess)
        {
            _onLoginSuccess = onLoginSuccess;
            LoginCommand = new RelayCommand(ExecuteLogin, CanLogin);
        }

        // ── COMMAND LOGIC ─────────────────────────────────────
        private bool CanLogin() =>
            !string.IsNullOrWhiteSpace(Username) &&
            !string.IsNullOrWhiteSpace(Password);

        private void ExecuteLogin()
        {
            User? loggedInUser = null;

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    string query = "SELECT * FROM Users WHERE Username = @username AND Password = @password";
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@username", Username.Trim());
                        command.Parameters.AddWithValue("@password", Password.Trim());
                        connection.Open();

                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                loggedInUser = new User
                                {
                                    Id = (int)reader["Id"],
                                    Username = reader["Username"].ToString()!,
                                    Password = reader["Password"].ToString()!
                                };
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Database connection failed: " + ex.Message);
                return;
            }

            if (loggedInUser != null)
            {
                _onLoginSuccess(loggedInUser);
            }
            else
            {
                MessageBox.Show("Invalid Username or Password.", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}