using System;
using StandNatura.Commands;
using StandNatura.Models;
using System.Windows.Input;

namespace StandNatura.ViewModels
{
    public class HotspotMapViewModel : ContributorBaseViewModel
    {
        // ── ACTIVE PAGE KEY ───────────────────────────────────
        public override string ActivePageKey => "HotspotMap";

        // ── COMMANDS ──────────────────────────────────────────
        public ICommand GoBackCommand { get; }

        // ── CONSTRUCTOR ───────────────────────────────────────
        public HotspotMapViewModel(Action<BaseViewModel> navigate, User currentUser, Action onLogout)
            : base(navigate, currentUser, onLogout)
        {
            GoBackCommand = new RelayCommand(() => _navigate(new ContributorHomeViewModel(_navigate, _currentUser, _onLogout)));
        }
    }
}