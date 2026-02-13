using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;

namespace GroupeV
{
    /// <summary>
    /// Fenêtre de connexion — Interface Neumorphic d'authentification pour les vendeurs
    /// </summary>
    public partial class LoginWindow : Window
    {
        private int _loginAttempts;
        private DateTime _lockoutUntil = DateTime.MinValue;
        private const int MaxAttempts = 5;
        private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(2);

        public LoginWindow()
        {
            InitializeComponent();
            EmailTextBox.Focus();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            AnimateElementsIn();
        }

        private void AnimateElementsIn()
        {
            AnimateElementWithDelay(EmailPanel, 0.2);
            AnimateElementWithDelay(PasswordPanel, 0.35);
            AnimateElementWithDelay(LoginButton, 0.5);
        }

        private async void AnimateElementWithDelay(FrameworkElement element, double delaySeconds)
        {
            element.Opacity = 0;
            await System.Threading.Tasks.Task.Delay(TimeSpan.FromSeconds(delaySeconds));

            var fadeIn = new DoubleAnimation
            {
                From = 0, To = 1, Duration = TimeSpan.FromSeconds(0.5)
            };
            var slideIn = new DoubleAnimation
            {
                From = 24, To = 0, Duration = TimeSpan.FromSeconds(0.5),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            element.BeginAnimation(OpacityProperty, fadeIn);
            if (element.RenderTransform is TranslateTransform transform)
                transform.BeginAnimation(TranslateTransform.YProperty, slideIn);
        }

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            // Anti brute-force : verrouillage temporaire
            if (DateTime.Now < _lockoutUntil)
            {
                var remaining = (_lockoutUntil - DateTime.Now).TotalSeconds;
                ShowStatus($"Trop de tentatives. Réessayez dans {remaining:F0}s", isError: true);
                ShakeElement(StatusBorder);
                return;
            }

            var email = EmailTextBox.Text.Trim();
            var password = PasswordBox.Password;

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                ShowStatus("Veuillez entrer l'email et le mot de passe", isError: true);
                ShakeElement(StatusBorder);
                return;
            }

            // Validation du format email
            if (!email.Contains('@') || !email.Contains('.') || email.Length > 255)
            {
                ShowStatus("Format d'adresse email invalide", isError: true);
                ShakeElement(StatusBorder);
                return;
            }

            var button = sender as Button;
            if (button != null) button.IsEnabled = false;
            ShowStatus("Authentification en cours...", isError: false);

            #if DEBUG
            // En mode debug, exécuter les diagnostics en premier
            var diagnostic = await LoginDiagnostics.DiagnoseLoginAsync(email, password);
            System.Diagnostics.Debug.WriteLine(diagnostic.ToString());
            
            if (!diagnostic.LoginSuccess)
            {
                ShowStatus($"{diagnostic.FailureReason}", isError: true);
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
                    ShowStatus($"Échec de la connexion à la base de données: {connectionCheck.message}", isError: true);
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
                    _loginAttempts++;
                    if (_loginAttempts >= MaxAttempts)
                        _lockoutUntil = DateTime.Now.Add(LockoutDuration);
                    ShowStatus("Email ou mot de passe invalide", isError: true);
                    ShakeElement(StatusBorder);
                    return;
                }

                // Vérifier si l'utilisateur est un vendeur
                var seller = await context.Vendeurs
                    .Include(v => v.Utilisateur)
                    .FirstOrDefaultAsync(v => v.IdUser == user.IdUser);

                if (seller == null)
                {
                    ShowStatus("Accès refusé. Compte vendeur requis.", isError: true);
                    ShakeElement(StatusBorder);
                    return;
                }

                // Vérifier le mot de passe (supporte les mots de passe hachés: BCrypt, SHA-256, SHA-512, MD5, et texte brut)
                if (!PasswordVerifier.VerifyPassword(password, user.MotDePasse))
                {
                    _loginAttempts++;
                    if (_loginAttempts >= MaxAttempts)
                        _lockoutUntil = DateTime.Now.Add(LockoutDuration);
                    #if DEBUG
                    var hashType = PasswordVerifier.GetHashTypeName(user.MotDePasse);
                    ShowStatus($"Échec de la vérification du mot de passe. Type de hachage: {hashType}", isError: true);
                    System.Diagnostics.Debug.WriteLine($"[CONNEXION] Type de hachage du mot de passe: {hashType}");
                    #else
                    ShowStatus("Email ou mot de passe invalide", isError: true);
                    #endif
                    ShakeElement(StatusBorder);
                    return;
                }

                // Connexion réussie — réinitialiser le compteur
                _loginAttempts = 0;

                // Tous les vendeurs ont accès - vérification de certification supprimée
                ShowStatus("Connexion réussie !", isError: false);

                // Stocker l'utilisateur actuel en session sécurisée
                AuthenticationService.SetSession(user, seller);

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
                ShowStatus(errorMessage, isError: true);
                ShakeElement(StatusBorder);
            }
            catch (DbUpdateException dbEx)
            {
                ShowStatus($"Erreur de mise à jour de la base de données: {dbEx.InnerException?.Message ?? dbEx.Message}", isError: true);
                ShakeElement(StatusBorder);
            }
            catch (Exception ex)
            {
                ShowStatus($"Erreur de connexion: {ex.Message}", isError: true);
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
                Controls.NeuDialog.ShowInfo(this, "Liste des vendeurs", sellers);
            }
            catch (Exception ex)
            {
                Controls.NeuDialog.ShowError(this, "Erreur de diagnostic", ex.Message);
            }
        }

        /// <summary>
        /// Afficher un message de statut avec animation
        /// </summary>
        private void ShowStatus(string message, bool isError)
        {
            StatusTextBlock.Text = message;
            var brush = isError
                ? (SolidColorBrush)FindResource("NeuDangerBrush")
                : (SolidColorBrush)FindResource("NeuSuccessBrush");
            StatusTextBlock.Foreground = brush;
            StatusBorder.Visibility = Visibility.Visible;

            var fadeIn = new DoubleAnimation { From = 0, To = 1, Duration = TimeSpan.FromSeconds(0.3) };
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
    /// Service d'authentification avec gestion de session sécurisée.
    /// Timeout automatique après inactivité.
    /// </summary>
    public static class AuthenticationService
    {
        private static readonly TimeSpan SessionTimeout = TimeSpan.FromMinutes(30);
        private static DateTime _lastActivity = DateTime.MinValue;

        public static Utilisateur? CurrentUser { get; set; }
        public static Vendeur? CurrentSeller { get; set; }

        public static bool IsAuthenticated
        {
            get
            {
                if (CurrentUser == null || CurrentSeller == null)
                    return false;

                if (DateTime.Now - _lastActivity > SessionTimeout)
                {
                    Logout();
                    return false;
                }

                _lastActivity = DateTime.Now;
                return true;
            }
        }

        /// <summary>
        /// Définit la session après une authentification réussie.
        /// </summary>
        public static void SetSession(Utilisateur user, Vendeur seller)
        {
            ArgumentNullException.ThrowIfNull(user);
            ArgumentNullException.ThrowIfNull(seller);
            CurrentUser = user;
            CurrentSeller = seller;
            _lastActivity = DateTime.Now;
        }

        /// <summary>
        /// Rafraîchit le timer de session.
        /// </summary>
        public static void RefreshSession()
        {
            if (CurrentUser != null)
                _lastActivity = DateTime.Now;
        }

        public static void Logout()
        {
            CurrentUser = null;
            CurrentSeller = null;
            _lastActivity = DateTime.MinValue;
        }
    }
}
