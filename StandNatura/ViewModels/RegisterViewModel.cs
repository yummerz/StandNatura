using System;
using System.Windows.Input;
using System.Windows;
using StandNatura.Commands;
using StandNatura.Models;
using Microsoft.Data.SqlClient;

namespace StandNatura.ViewModels
{
    public class RegisterViewModel : BaseViewModel
    {
        private readonly Action<BaseViewModel> _onBackToLogin;
        private readonly Action<BaseViewModel> _navigate;
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

        private string _confirmPassword = string.Empty;
        public string ConfirmPassword
        {
            get => _confirmPassword;
            set => SetProperty(ref _confirmPassword, value);
        }

        // ── COMMANDS ──────────────────────────────────────────
        public ICommand RegisterCommand { get; }
        public ICommand GoBackCommand { get; }

        // ── CONSTRUCTOR ───────────────────────────────────────
        public RegisterViewModel(Action<BaseViewModel> navigate, Action<User> onLoginSuccess)
        {
            _onLoginSuccess = onLoginSuccess;
            _navigate = navigate;
            RegisterCommand = new RelayCommand(ExecuteRegister, CanRegister);
            GoBackCommand = new RelayCommand(() => _navigate(new LoginViewModel(_onLoginSuccess, _navigate)));
        }

        // ── REGISTER ──────────────────────────────────────────
        private void ExecuteRegister()
        {
            if (Password != ConfirmPassword)
            {
                MessageBox.Show("Passwords do not match.", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    string query = "INSERT INTO Users (Username, Password, Role) VALUES (@username, @password, 'Contributor')";
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@username", Username.Trim());
                        command.Parameters.AddWithValue("@password", Password.Trim());
                        connection.Open();
                        command.ExecuteNonQuery();
                    }
                }

                MessageBox.Show("Account created successfully! You can now log in.", "Success",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                _navigate(new LoginViewModel(_onLoginSuccess, _navigate));
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("UNIQUE"))
                    MessageBox.Show("That username is already taken. Please choose another.", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                else
                    MessageBox.Show("Failed to register: " + ex.Message);
            }
        }

        // ── CAN EXECUTE ───────────────────────────────────────
        private bool CanRegister() =>
            !string.IsNullOrWhiteSpace(Username) &&
            !string.IsNullOrWhiteSpace(Password) &&
            !string.IsNullOrWhiteSpace(ConfirmPassword);
    }
}