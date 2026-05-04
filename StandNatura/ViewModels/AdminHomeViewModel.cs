using System;
using StandNatura.Commands;
using StandNatura.Models;
using System.Windows.Input;

namespace StandNatura.ViewModels
{
    public class AdminHomeViewModel : BaseViewModel
    {
        private readonly Action<BaseViewModel> _navigate;
        private readonly User _currentUser;

        public ICommand GoToVerifyPostsCommand { get; }
        public ICommand GoToManageUsersCommand { get; }
        public ICommand GoToSightingFeedCommand { get; }
        public ICommand GoToDonationsFundsCommand { get; }

        public AdminHomeViewModel(Action<BaseViewModel> navigate, User currentUser)
        {
            _navigate = navigate;
            _currentUser = currentUser;

            GoToVerifyPostsCommand = new RelayCommand(() => _navigate(new VerifyPostsViewModel(_navigate, _currentUser)));
            GoToManageUsersCommand = new RelayCommand(() => _navigate(new ManageUsersViewModel(_navigate, _currentUser)));
            GoToSightingFeedCommand = new RelayCommand(() => _navigate(new SightingFeedViewModel(_navigate, _currentUser)));
            GoToDonationsFundsCommand = new RelayCommand(() => _navigate(new DonationsFundsViewModel(_navigate, _currentUser)));
        }
    }
}