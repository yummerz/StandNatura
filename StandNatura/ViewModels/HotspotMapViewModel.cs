using System;
using StandNatura.Commands;
using StandNatura.Models;
using System.Windows.Input;

namespace StandNatura.ViewModels
{
    public class HotspotMapViewModel : BaseViewModel
    {
        private readonly Action<BaseViewModel> _navigate;
        private readonly User _currentUser;
        private readonly Action _onLogout;
        public ICommand GoBackCommand { get; }

        public HotspotMapViewModel(Action<BaseViewModel> navigate, User currentUser, Action onLogout)
        {
            _navigate = navigate;
            _currentUser = currentUser;
            _onLogout = onLogout;
            GoBackCommand = new RelayCommand(() => _navigate(new ContributorHomeViewModel(_navigate, _currentUser, _onLogout)));
        }
    }
}