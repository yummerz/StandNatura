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
            DataContext = new LoginViewModel(OnLoginSuccess);
        }

        private void OnLoginSuccess(User user)
        {
            _currentUser = user;
            DataContext = new AdminHomeViewModel(Navigate, _currentUser);
        }

        private void Navigate(BaseViewModel viewModel)
        {
            DataContext = viewModel;
        }
    }
}