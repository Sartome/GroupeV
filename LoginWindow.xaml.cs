using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;

namespace GroupeV
{
    /// <summary>
    /// Fenêtre de connexion - Interface d'authentification pour les vendeurs
    /// </summary>
    public partial class LoginWindow : Window
    {
        private readonly Random _random = new();
        
        public LoginWindow()
        {
            InitializeComponent();
            EmailTextBox.Focus();
        }

        /// <summary>
        /// Animations au chargement de la fenêtre
        /// </summary>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Créer des particules animées
            CreateAnimatedParticles();
            
            // Animer l'entrée des éléments
            AnimateElementsIn();
        }

        /// <summary>
        /// Créer des particules animées en arrière-plan
        /// </summary>
        private void CreateAnimatedParticles()
        {
            for (int i = 0; i < 25; i++)
            {
                var particle = new Ellipse
                {
                    Width = _random.Next(3, 10),
                    Height = _random.Next(3, 10),
                    Fill = new SolidColorBrush(Color.FromArgb(
                        (byte)_random.Next(50, 150),
                        0,
                        (byte)_random.Next(150, 255),
                        0)),
                    Opacity = 0.3
                };

                Canvas.SetLeft(particle, _random.Next(0, (int)this.Width));
                Canvas.SetTop(particle, _random.Next(0, (int)this.Height));
                
                ParticlesCanvas.Children.Add(particle);

                // Animation de déplacement
                var moveY = new DoubleAnimation
                {
                    From = Canvas.GetTop(particle),
                    To = _random.Next(0, (int)this.Height),
                    Duration = TimeSpan.FromSeconds(_random.Next(5, 12)),
                    RepeatBehavior = RepeatBehavior.Forever,
                    AutoReverse = true
                };

                var moveX = new DoubleAnimation
                {
                    From = Canvas.GetLeft(particle),
                    To = _random.Next(0, (int)this.Width),
                    Duration = TimeSpan.FromSeconds(_random.Next(5, 12)),
                    RepeatBehavior = RepeatBehavior.Forever,
                    AutoReverse = true
                };

                // Animation d'opacité
                var fade = new DoubleAnimation
                {
                    From = 0.1,
                    To = 0.6,
                    Duration = TimeSpan.FromSeconds(_random.Next(2, 4)),
                    RepeatBehavior = RepeatBehavior.Forever,
                    AutoReverse = true
                };

                particle.BeginAnimation(Canvas.TopProperty, moveY);
                particle.BeginAnimation(Canvas.LeftProperty, moveX);
                particle.BeginAnimation(OpacityProperty, fade);
            }
        }

        /// <summary>
        /// Animer l'entrée des éléments
        /// </summary>
        private void AnimateElementsIn()
        {
            // Animation du header
            var headerFade = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromSeconds(0.8),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            HeaderBorder.BeginAnimation(OpacityProperty, headerFade);

            // Animation des champs avec délai
            AnimateElementWithDelay(EmailPanel, 0.3);
            AnimateElementWithDelay(PasswordPanel, 0.5);
            AnimateElementWithDelay(LoginButton, 0.7);
        }

        /// <summary>
        /// Animer un élément avec un délai
        /// </summary>
        private async void AnimateElementWithDelay(FrameworkElement element, double delaySeconds)
        {
            element.Opacity = 0;
            await System.Threading.Tasks.Task.Delay(TimeSpan.FromSeconds(delaySeconds));

            var fadeIn = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromSeconds(0.5)
            };

            var slideIn = new DoubleAnimation
            {
                From = 30,
                To = 0,
                Duration = TimeSpan.FromSeconds(0.5),
                EasingFunction = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.3 }
            };

            element.BeginAnimation(OpacityProperty, fadeIn);
            
            if (element.RenderTransform is TranslateTransform transform)
            {
                transform.BeginAnimation(TranslateTransform.YProperty, slideIn);
            }
        }

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            var email = EmailTextBox.Text.Trim();
            var password = PasswordBox.Password;

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                ShowStatus("?? Veuillez entrer l'email et le mot de passe", isError: true);
                ShakeElement(StatusBorder);
                return;
            }

            var button = sender as Button;
            if (button != null) button.IsEnabled = false;
            ShowStatus("?? Authentification en cours...", isError: false);

            #if DEBUG
            // En mode debug, exécuter les diagnostics en premier
            var diagnostic = await LoginDiagnostics.DiagnoseLoginAsync(email, password);
            System.Diagnostics.Debug.WriteLine(diagnostic.ToString());
            
            if (!diagnostic.LoginSuccess)
            {
                ShowStatus($"? {diagnostic.FailureReason}", isError: true);
                ShakeElement(StatusBorder);
                if (button != null) button.IsEnabled = true;
                return;
            }
            #endif

            try
            {
                // Vérifier d'abord la connexion à la base de données
                var connectionCheck = await DatabaseHelper.CheckConnectionAsync();
                if (!connectionCheck.success)
                {
                    ShowStatus($"? Échec de la connexion à la base de données: {connectionCheck.message}", isError: true);
                    ShakeElement(StatusBorder);
                    return;
                }

                using var context = new DatabaseContext();

                // Trouver l'utilisateur par email (insensible à la casse)
                var emailLower = email.ToLowerInvariant();
                var user = await context.Utilisateurs
                    .FirstOrDefaultAsync(u => u.Email != null && u.Email.ToLower() == emailLower);

                if (user == null)
                {
                    ShowStatus("? Email ou mot de passe invalide", isError: true);
                    ShakeElement(StatusBorder);
                    return;
                }

                // Vérifier si l'utilisateur est un vendeur
                var seller = await context.Vendeurs
                    .Include(v => v.Utilisateur)
                    .FirstOrDefaultAsync(v => v.IdUser == user.IdUser);

                if (seller == null)
                {
                    ShowStatus("? Accès refusé. Compte vendeur requis.", isError: true);
                    ShakeElement(StatusBorder);
                    return;
                }

                // Vérifier le mot de passe (supporte les mots de passe hachés: BCrypt, SHA-256, SHA-512, MD5, et texte brut)
                if (!PasswordVerifier.VerifyPassword(password, user.MotDePasse))
                {
                    #if DEBUG
                    var hashType = PasswordVerifier.GetHashTypeName(user.MotDePasse);
                    ShowStatus($"? Échec de la vérification du mot de passe. Type de hachage: {hashType}", isError: true);
                    System.Diagnostics.Debug.WriteLine($"[CONNEXION] Type de hachage du mot de passe: {hashType}");
                    #else
                    ShowStatus("? Email ou mot de passe invalide", isError: true);
                    #endif
                    ShakeElement(StatusBorder);
                    return;
                }

                // Tous les vendeurs ont accès - vérification de certification supprimée
                ShowStatus("? Connexion réussie !", isError: false);

                // Stocker l'utilisateur actuel en session
                AuthenticationService.CurrentUser = user;
                AuthenticationService.CurrentSeller = seller;

                // Ouvrir la fenêtre principale
                await System.Threading.Tasks.Task.Delay(500); // Brève pause pour afficher le succès
                var mainWindow = new MainWindow();
                mainWindow.Show();
                this.Close();
            }
            catch (MySqlException mysqlEx)
            {
                // Erreurs MySQL spécifiques
                var errorMessage = mysqlEx.Number switch
                {
                    0 => "Impossible de se connecter au serveur de base de données. Vérifiez si MySQL est en cours d'exécution.",
                    1042 => "Impossible de résoudre l'hôte de la base de données. Vérifiez votre chaîne de connexion.",
                    1045 => "Accès refusé. Vérifiez le nom d'utilisateur et le mot de passe de la base de données.",
                    1049 => "La base de données 'vente_groupe' n'existe pas. Veuillez créer la base de données en premier.",
                    _ => $"Erreur de base de données (Code {mysqlEx.Number}): {mysqlEx.Message}"
                };
                ShowStatus($"? {errorMessage}", isError: true);
                ShakeElement(StatusBorder);
            }
            catch (DbUpdateException dbEx)
            {
                ShowStatus($"? Erreur de mise à jour de la base de données: {dbEx.InnerException?.Message ?? dbEx.Message}", isError: true);
                ShakeElement(StatusBorder);
            }
            catch (Exception ex)
            {
                ShowStatus($"? Erreur de connexion: {ex.Message}", isError: true);
                ShakeElement(StatusBorder);
            }
            finally
            {
                if (button != null) button.IsEnabled = true;
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        /// <summary>
        /// Afficher les informations de diagnostic - pour déboguer les problèmes de connexion
        /// </summary>
        private async void ShowDiagnostics_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var sellers = await LoginDiagnostics.ListAllSellersAsync();
                MessageBox.Show(sellers, "Liste des vendeurs", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur: {ex.Message}", "Erreur de diagnostic", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Afficher un message de statut avec animation
        /// </summary>
        private void ShowStatus(string message, bool isError)
        {
            StatusTextBlock.Text = message;
            StatusBorder.BorderBrush = isError ? 
                System.Windows.Media.Brushes.Red : 
                System.Windows.Media.Brushes.LimeGreen;
            StatusTextBlock.Foreground = isError ? 
                System.Windows.Media.Brushes.Red : 
                System.Windows.Media.Brushes.LimeGreen;
            StatusBorder.Visibility = Visibility.Visible;

            // Animation d'apparition
            var fadeIn = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromSeconds(0.3)
            };
            StatusBorder.BeginAnimation(OpacityProperty, fadeIn);
        }

        /// <summary>
        /// Animation de tremblement pour les erreurs
        /// </summary>
        private void ShakeElement(FrameworkElement element)
        {
            var transform = new TranslateTransform();
            element.RenderTransform = transform;

            var animation = new DoubleAnimationUsingKeyFrames();
            animation.KeyFrames.Add(new LinearDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0))));
            animation.KeyFrames.Add(new LinearDoubleKeyFrame(-10, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0.05))));
            animation.KeyFrames.Add(new LinearDoubleKeyFrame(10, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0.1))));
            animation.KeyFrames.Add(new LinearDoubleKeyFrame(-10, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0.15))));
            animation.KeyFrames.Add(new LinearDoubleKeyFrame(10, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0.2))));
            animation.KeyFrames.Add(new LinearDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0.25))));

            transform.BeginAnimation(TranslateTransform.XProperty, animation);
        }
    }

    /// <summary>
    /// Service d'authentification simple pour stocker la session utilisateur actuelle
    /// En production, utilisez une gestion de session appropriée
    /// </summary>
    public static class AuthenticationService
    {
        public static Utilisateur? CurrentUser { get; set; }
        public static Vendeur? CurrentSeller { get; set; }

        public static bool IsAuthenticated => CurrentUser != null && CurrentSeller != null;

        public static void Logout()
        {
            CurrentUser = null;
            CurrentSeller = null;
        }
    }
}
