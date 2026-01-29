using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace GroupeV
{
    /// <summary>
    /// Ecran de demarrage chaotique ultra-anime
    /// </summary>
    public partial class SplashScreen : Window
    {
        private readonly Random _random = new();
        
        public SplashScreen()
        {
            InitializeComponent();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Creer des particules chaotiques en arriere-plan
            CreateChaoticsParticles();
            
            // Lancer l'animation de chargement
            await AnimateLoadingAsync();
            
            // Ouvrir la fenetre de connexion
            var loginWindow = new LoginWindow();
            loginWindow.Show();
            this.Close();
        }

        /// <summary>
        /// Creer des particules animees chaotiques
        /// </summary>
        private void CreateChaoticsParticles()
        {
            for (int i = 0; i < 30; i++)
            {
                var particle = new Ellipse
                {
                    Width = _random.Next(5, 15),
                    Height = _random.Next(5, 15),
                    Fill = new SolidColorBrush(Color.FromArgb(
                        (byte)_random.Next(100, 255),
                        0,
                        (byte)_random.Next(200, 255),
                        0))
                };

                Canvas.SetLeft(particle, _random.Next(0, (int)this.Width));
                Canvas.SetTop(particle, _random.Next(0, (int)this.Height));
                
                ParticlesCanvas.Children.Add(particle);

                // Animation de deplacement chaotique
                var moveAnimation = new DoubleAnimation
                {
                    From = Canvas.GetTop(particle),
                    To = _random.Next(0, (int)this.Height),
                    Duration = TimeSpan.FromSeconds(_random.Next(3, 8)),
                    RepeatBehavior = RepeatBehavior.Forever,
                    AutoReverse = true
                };

                var moveAnimation2 = new DoubleAnimation
                {
                    From = Canvas.GetLeft(particle),
                    To = _random.Next(0, (int)this.Width),
                    Duration = TimeSpan.FromSeconds(_random.Next(3, 8)),
                    RepeatBehavior = RepeatBehavior.Forever,
                    AutoReverse = true
                };

                // Animation d'opacite
                var fadeAnimation = new DoubleAnimation
                {
                    From = 0.2,
                    To = 1.0,
                    Duration = TimeSpan.FromSeconds(_random.Next(1, 3)),
                    RepeatBehavior = RepeatBehavior.Forever,
                    AutoReverse = true
                };

                particle.BeginAnimation(Canvas.TopProperty, moveAnimation);
                particle.BeginAnimation(Canvas.LeftProperty, moveAnimation2);
                particle.BeginAnimation(OpacityProperty, fadeAnimation);
            }
        }

        /// <summary>
        /// Animation de la barre de chargement avec messages
        /// </summary>
        private async Task AnimateLoadingAsync()
        {
            // Messages de chargement
            var messages = new[]
            {
                ("?? Connexion à la base de données...", Msg1),
                ("?? Chargement des produits...", Msg2),
                ("?? Initialisation de l'interface...", Msg3),
                ("? Configuration du système...", Msg4)
            };

            var totalDuration = 4000; // 4 secondes
            var stepDuration = totalDuration / messages.Length;

            for (int i = 0; i < messages.Length; i++)
            {
                // Mettre a jour le texte de statut
                StatusText.Text = messages[i].Item1;

                // Animer la barre de progression
                var animation = new DoubleAnimation
                {
                    To = (i + 1) * (400.0 / messages.Length),
                    Duration = TimeSpan.FromMilliseconds(stepDuration),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
                };
                LoadingBar.BeginAnimation(WidthProperty, animation);

                // Faire apparaitre le message
                var fadeIn = new DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    Duration = TimeSpan.FromMilliseconds(300)
                };
                messages[i].Item2.BeginAnimation(OpacityProperty, fadeIn);

                // Attendre avant le prochain message
                await Task.Delay(stepDuration);
            }

            // Message final
            StatusText.Text = "? PRET !";
            LoadingText.Text = "?? CHARGEMENT TERMINE !";
            
            // Animation finale de la barre (remplir completement)
            var finalAnimation = new DoubleAnimation
            {
                To = 400,
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = new BounceEase { EasingMode = EasingMode.EaseOut, Bounces = 3, Bounciness = 2 }
            };
            LoadingBar.BeginAnimation(WidthProperty, finalAnimation);

            await Task.Delay(800);
        }
    }
}
