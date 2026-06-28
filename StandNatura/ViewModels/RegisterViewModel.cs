using System;
using System.Windows.Input;
using System.Windows;
using StandNatura.Commands;
using StandNatura.Models;
using StandNatura.Services;
using Microsoft.Data.SqlClient;

namespace StandNatura.ViewModels
{
    public class RegisterViewModel : BaseViewModel
    {
        private readonly Action<BaseViewModel> _onBackToLogin;
        private readonly Action<BaseViewModel> _navigate;
        private readonly Action<User> _onLoginSuccess;
        private static readonly string connectionString = DatabaseConfig.ConnectionString;
        private string? _captchaToken;

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
        public ICommand GoogleSignInCommand { get; }

        // ── CONSTRUCTOR ───────────────────────────────────────
        public RegisterViewModel(Action<BaseViewModel> navigate, Action<User> onLoginSuccess)
        {
            _onLoginSuccess = onLoginSuccess;
            _navigate = navigate;
            RegisterCommand = new RelayCommand(ExecuteRegister, CanRegister);
            GoBackCommand = new RelayCommand(() => _navigate(new LoginViewModel(_onLoginSuccess, _navigate)));
            // Same shared flow as the Login screen's Google button.
            GoogleSignInCommand = new AsyncRelayCommand(() => GoogleSignInService.SignInAsync(_onLoginSuccess));
        }

        // ── REGISTER ──────────────────────────────────────────
        private async void ExecuteRegister()
        {
            if (Password != ConfirmPassword)
            {
                MessageBox.Show("Passwords do not match.", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Verify the reCAPTCHA with Google before creating any account.
            bool captchaOk;
            try
            {
                captchaOk = await RecaptchaService.VerifyAsync(_captchaToken);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not verify the CAPTCHA: " + ex.Message, "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                ResetCaptcha();
                return;
            }

            if (!captchaOk)
            {
                MessageBox.Show("CAPTCHA verification failed (it may have expired). " +
                    "Please complete the \"I'm not a robot\" check again.", "Verification Failed",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                ResetCaptcha();
                return;
            }

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    var (hash, salt) = PasswordHasher.HashPassword(Password.Trim());
                    string query = "INSERT INTO Users (Username, PasswordHash, Salt, Role) VALUES (@username, @hash, @salt, 'Contributor')";
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@username", Username.Trim());
                        command.Parameters.AddWithValue("@hash", hash);
                        command.Parameters.AddWithValue("@salt", salt);
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
            !string.IsNullOrWhiteSpace(ConfirmPassword) &&
            !string.IsNullOrEmpty(_captchaToken);

        // ── CAPTCHA ───────────────────────────────────────────
        // True once the popup returns a token; drives the button text and gating.
        public bool IsCaptchaVerified => !string.IsNullOrEmpty(_captchaToken);
        public string CaptchaButtonText =>
            IsCaptchaVerified ? "✓ Verification complete" : "\U0001F512 Complete verification";

        // Called from the View: a token when the popup is solved, null when it is
        // dismissed, cleared, or expires. Gates the Create Account button above.
        public void SetCaptchaToken(string? token)
        {
            _captchaToken = token;
            OnPropertyChanged(nameof(IsCaptchaVerified));
            OnPropertyChanged(nameof(CaptchaButtonText));
            // Changed outside a UI event, so nudge WPF to re-evaluate CanRegister.
            CommandManager.InvalidateRequerySuggested();
        }

        private void ResetCaptcha() => SetCaptchaToken(null);
    }
}