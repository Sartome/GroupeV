using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace GroupeV
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void SignInButton_Click(object sender, RoutedEventArgs e)
        {
            var username = UsernameTextBox.Text?.Trim();
            var password = PasswordBox.Password;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show(this, "Enter username and password.", "Sign in", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Simple local check for demo purposes. Replace with real auth as needed.
            if (username == "admin" && password == "password")
            {
                StatusTextBlock.Text = $"Signed in as {username}";
                ShopButton.IsEnabled = true;
                StocksButton.IsEnabled = true;
                SignInButton.IsEnabled = false;
                UsernameTextBox.IsEnabled = false;
                PasswordBox.IsEnabled = false;
            }
            else
            {
                MessageBox.Show(this, "Invalid username or password.", "Sign in", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ShopButton_Click(object sender, RoutedEventArgs e)
        {
            var win = new ShopWindow();
            win.Owner = this;
            win.ShowDialog();
        }

        private void StocksButton_Click(object sender, RoutedEventArgs e)
        {
            var win = new StocksWindow();
            win.Owner = this;
            win.ShowDialog();
        }
    }
}