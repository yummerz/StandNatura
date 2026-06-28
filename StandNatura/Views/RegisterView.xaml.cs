using System.Windows;
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

        private void PasswordInput_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is RegisterViewModel vm)
                vm.Password = PasswordInput.Password;
        }

        private void ConfirmPasswordInput_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is RegisterViewModel vm)
                vm.ConfirmPassword = ConfirmPasswordInput.Password;
        }

        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is RegisterViewModel vm)
            {
                vm.Password = PasswordInput.Password;
                vm.ConfirmPassword = ConfirmPasswordInput.Password;
            }
        }

        // Opens the reCAPTCHA popup window; when it closes, hands the captured token
        // (or null if dismissed) to the ViewModel, which updates the button gating
        // and the verification status text.
        private void OpenCaptcha_Click(object sender, RoutedEventArgs e)
        {
            var popup = new CaptchaWindow { Owner = Window.GetWindow(this) };
            popup.ShowDialog();
            if (DataContext is RegisterViewModel vm)
                vm.SetCaptchaToken(popup.Token);
        }
    }
}
