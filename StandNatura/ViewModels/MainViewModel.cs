namespace StandNatura.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        private string _welcomeMessage = string.Empty;
        public string WelcomeMessage
        {
            get => _welcomeMessage;
            set => SetProperty(ref _welcomeMessage, value);
        }

        public MainViewModel(string username)
        {
            WelcomeMessage = $"Welcome, {username}!";
        }
    }
}