using System.Windows;

namespace StandNatura.Views
{
    public partial class ArchiveReasonDialog : Window
    {
        public string Reason { get; private set; } = string.Empty;

        public ArchiveReasonDialog()
        {
            InitializeComponent();
        }

        private void Confirm_Click(object sender, RoutedEventArgs e)
        {
            Reason = ReasonTextBox.Text?.Trim() ?? string.Empty;
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}