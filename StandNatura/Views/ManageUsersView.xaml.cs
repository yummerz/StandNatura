using System.Windows.Controls;
using StandNatura.ViewModels;

namespace StandNatura.Views
{
    public partial class ManageUsersView : UserControl
    {
        public ManageUsersView()
        {
            InitializeComponent();
            NewPasswordInput.PasswordChanged += NewPasswordInput_PasswordChanged;
        }

        private void NewPasswordInput_PasswordChanged(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is ManageUsersViewModel vm)
            {
                vm.NewPassword = NewPasswordInput.Password;
            }
        }

        private void AddUserButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is ManageUsersViewModel vm)
            {
                vm.NewPassword = NewPasswordInput.Password;
            }
        }
    }
}