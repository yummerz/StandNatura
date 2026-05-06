using StandNatura.Models;
using StandNatura.ViewModels;
using System.Windows;

namespace StandNatura
{
    public partial class MainWindow : Window
    {
        private User? _currentUser;

        public MainWindow()
        {
            InitializeComponent();
            ShowLogin();
        }

        private void ShowLogin()
        {
            DataContext = new LoginViewModel(OnLoginSuccess, Navigate);
        }

        private void OnLoginSuccess(User user)
        {
            _currentUser = user;

            if (user.Role == "Admin")
                DataContext = new AdminHomeViewModel(Navigate, _currentUser, ShowLogin);
            else
                DataContext = new ContributorHomeViewModel(Navigate, _currentUser, ShowLogin);
        }

        private void Navigate(BaseViewModel viewModel)
        {
            DataContext = viewModel;
        }
    }
}