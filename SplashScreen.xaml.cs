using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Animation;

namespace GroupeV
{
    /// <summary>
    /// Écran de démarrage Neumorphic avec barre de progression fluide
    /// </summary>
    public partial class SplashScreen : Window
    {
        public SplashScreen()
        {
            InitializeComponent();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await AnimateLoadingAsync();

            var loginWindow = new LoginWindow();
            loginWindow.Show();
            Close();
        }

        private async Task AnimateLoadingAsync()
        {
            var messages = new[]
            {
                ("Connexion à la base de données...", Msg1),
                ("Chargement des produits...", Msg2),
                ("Initialisation de l'interface...", Msg3),
                ("Configuration du système...", Msg4)
            };

            int totalDuration = 3500;
            int stepDuration = totalDuration / messages.Length;

            for (int i = 0; i < messages.Length; i++)
            {
                StatusText.Text = messages[i].Item1;

                var barAnimation = new DoubleAnimation
                {
                    To = (i + 1) * (360.0 / messages.Length),
                    Duration = TimeSpan.FromMilliseconds(stepDuration),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
                };
                LoadingBar.BeginAnimation(WidthProperty, barAnimation);

                var fadeIn = new DoubleAnimation { From = 0, To = 1, Duration = TimeSpan.FromMilliseconds(300) };
                messages[i].Item2.BeginAnimation(OpacityProperty, fadeIn);

                await Task.Delay(stepDuration);
            }

            StatusText.Text = "Prêt !";
            LoadingText.Text = "Chargement terminé";

            var finalAnim = new DoubleAnimation
            {
                To = 360,
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            LoadingBar.BeginAnimation(WidthProperty, finalAnim);

            await Task.Delay(600);
        }
    }
}
