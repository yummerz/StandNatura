using System.Windows.Controls;
using StandNatura.ViewModels;

namespace StandNatura.Views
{
    public partial class RegisterView : UserControl
    {
        public RegisterView()
        {
            InitializeComponent();
            PasswordInput.PasswordChanged += PasswordInput_PasswordChanged;
            ConfirmPasswordInput.PasswordChanged += ConfirmPasswordInput_PasswordChanged;
        }

        private void PasswordInput_PasswordChanged(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is RegisterViewModel vm)
                vm.Password = PasswordInput.Password;
        }

        private void ConfirmPasswordInput_PasswordChanged(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is RegisterViewModel vm)
                vm.ConfirmPassword = ConfirmPasswordInput.Password;
        }

        private void RegisterButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is RegisterViewModel vm)
            {
                vm.Password = PasswordInput.Password;
                vm.ConfirmPassword = ConfirmPasswordInput.Password;
            }
        }
    }
}