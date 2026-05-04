using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows;
using StandNatura.Commands;
using StandNatura.Models;
using Microsoft.Data.SqlClient;

namespace StandNatura.ViewModels
{
    public class ManageUsersViewModel : BaseViewModel
    {
        private readonly Action<BaseViewModel> _navigate;
        private readonly User _currentUser;
        private static readonly string connectionString = DatabaseConfig.ConnectionString;

        // ── BINDABLE PROPERTIES ──────────────────────────────
        private ObservableCollection<User> _users = new();
        public List<string> Roles { get; } = new List<string> { "Contributor", "Admin" };
        public ObservableCollection<User> Users
        {
            get => _users;
            set => SetProperty(ref _users, value);
        }

        private User? _selectedUser;
        public User? SelectedUser
        {
            get => _selectedUser;
            set => SetProperty(ref _selectedUser, value);
        }

        private string _newUsername = string.Empty;
        public string NewUsername
        {
            get => _newUsername;
            set => SetProperty(ref _newUsername, value);
        }

        private string _newPassword = string.Empty;
        public string NewPassword
        {
            get => _newPassword;
            set => SetProperty(ref _newPassword, value);
        }

        private string _newRole = "Contributor";
        public string NewRole
        {
            get => _newRole;
            set => SetProperty(ref _newRole, value);
        }

        // ── COMMANDS ──────────────────────────────────────────
        public ICommand GoBackCommand { get; }
        public ICommand AddUserCommand { get; }
        public ICommand DeleteUserCommand { get; }

        // ── CONSTRUCTOR ───────────────────────────────────────
        public ManageUsersViewModel(Action<BaseViewModel> navigate, User currentUser)
        {
            _navigate = navigate;
            _currentUser = currentUser;

            GoBackCommand = new RelayCommand(() => _navigate(new AdminHomeViewModel(_navigate, _currentUser)));
            AddUserCommand = new RelayCommand(AddUser, CanAddUser);
            DeleteUserCommand = new RelayCommand(DeleteUser, CanDeleteUser);

            LoadUsers();
        }

        // ── LOAD DATA ─────────────────────────────────────────
        private void LoadUsers()
        {
            Users.Clear();

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    string query = "SELECT Id, Username, Role FROM Users";
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        connection.Open();
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                Users.Add(new User
                                {
                                    Id = (int)reader["Id"],
                                    Username = reader["Username"].ToString()!,
                                    Role = reader["Role"].ToString()!
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to load users: " + ex.Message);
            }
        }

        // ── ADD USER ──────────────────────────────────────────
        private void AddUser()
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    string query = "INSERT INTO Users (Username, Password, Role) VALUES (@username, @password, @role)";
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@username", NewUsername.Trim());
                        command.Parameters.AddWithValue("@password", NewPassword.Trim());
                        command.Parameters.AddWithValue("@role", NewRole);
                        connection.Open();
                        command.ExecuteNonQuery();
                    }
                }

                MessageBox.Show($"User '{NewUsername}' has been added successfully.", "Success",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                NewUsername = string.Empty;
                NewPassword = string.Empty;
                NewRole = "Contributor";
                LoadUsers();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to add user: " + ex.Message);
            }
        }

        // ── DELETE USER ───────────────────────────────────────
        private void DeleteUser()
        {
            if (SelectedUser!.Id == _currentUser.Id)
            {
                MessageBox.Show("You cannot delete your own account.", "Action Blocked",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show(
                $"Are you sure you want to delete '{SelectedUser.Username}'?\n\nThis action cannot be undone.",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    using (SqlConnection connection = new SqlConnection(connectionString))
                    {
                        string query = "DELETE FROM Users WHERE Id = @id";
                        using (SqlCommand command = new SqlCommand(query, connection))
                        {
                            command.Parameters.AddWithValue("@id", SelectedUser.Id);
                            connection.Open();
                            command.ExecuteNonQuery();
                        }
                    }

                    MessageBox.Show($"User '{SelectedUser.Username}' has been deleted.", "Success",
                        MessageBoxButton.OK, MessageBoxImage.Information);

                    SelectedUser = null;
                    LoadUsers();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Failed to delete user: " + ex.Message);
                }
            }
        }

        // ── CAN EXECUTE ───────────────────────────────────────
        private bool CanAddUser() =>
            !string.IsNullOrWhiteSpace(NewUsername) &&
            !string.IsNullOrWhiteSpace(NewPassword);

        private bool CanDeleteUser() => SelectedUser != null;
    }
}