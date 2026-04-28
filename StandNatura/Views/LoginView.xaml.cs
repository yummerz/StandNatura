using System.Windows.Controls;
using StandNatura.ViewModels;

namespace StandNatura.Views
{
    public partial class LoginView : UserControl
    {
        public LoginView()
        {
            InitializeComponent();
            PasswordInput.PasswordChanged += PasswordInput_PasswordChanged;
        }

        private void PasswordInput_PasswordChanged(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is LoginViewModel vm)
            {
                vm.Password = PasswordInput.Password;
            }
        }

        private void LoginButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is LoginViewModel vm)
            {
                vm.Password = PasswordInput.Password;
            }
        }
    }
}