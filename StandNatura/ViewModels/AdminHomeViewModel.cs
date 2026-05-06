using System;
using StandNatura.Commands;
using StandNatura.Models;
using System.Windows.Input;
using System.Windows;

namespace StandNatura.ViewModels
{
    public class AdminHomeViewModel : BaseViewModel
    {
        private readonly Action<BaseViewModel> _navigate;
        private readonly Action _onLogout;
        private readonly User _currentUser;

        public ICommand GoToVerifyPostsCommand { get; }
        public ICommand GoToManageUsersCommand { get; }
        public ICommand GoToSightingFeedCommand { get; }
        public ICommand GoToDonationsFundsCommand { get; }
        public ICommand LogoutCommand { get; }

        public AdminHomeViewModel(Action<BaseViewModel> navigate, User currentUser, Action onLogout)
        {
            _navigate = navigate;
            _currentUser = currentUser;
            _onLogout = onLogout;

            GoToVerifyPostsCommand = new RelayCommand(() => _navigate(new VerifyPostsViewModel(_navigate, _currentUser, _onLogout)));
            GoToManageUsersCommand = new RelayCommand(() => _navigate(new ManageUsersViewModel(_navigate, _currentUser, _onLogout)));
            GoToSightingFeedCommand = new RelayCommand(() => _navigate(new SightingFeedViewModel(_navigate, _currentUser, _onLogout)));
            GoToDonationsFundsCommand = new RelayCommand(() => _navigate(new DonationsFundsViewModel(_navigate, _currentUser, _onLogout)));
            LogoutCommand = new RelayCommand(Logout);
        }

        private void Logout()
        {
            var result = MessageBox.Show(
                "Are you sure you want to logout?",
                "Confirm Logout",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
                _onLogout();
        }
    }
}