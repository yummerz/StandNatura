using StandNatura.Models;
using StandNatura.ViewModels;
using System.Windows;

namespace StandNatura
{
    public partial class MainWindow : Window
    {
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
            DataContext = new MainViewModel(user.Username);
        }
    }
}