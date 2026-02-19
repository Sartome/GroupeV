using System.Windows;

namespace GroupeV
{
    public partial class NewTicketDialog : Window
    {
        public string Titre { get; private set; } = string.Empty;
        public string Description { get; private set; } = string.Empty;

        public NewTicketDialog()
        {
            InitializeComponent();
            TitreBox.Focus();
        }

        private void Create_Click(object sender, RoutedEventArgs e)
        {
            var titre = TitreBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(titre))
            {
                TitreBox.BorderBrush = (System.Windows.Media.SolidColorBrush)FindResource("NeuDangerBrush");
                return;
            }

            Titre = titre;
            Description = DescriptionBox.Text.Trim();
            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e) => Close();
    }
}
