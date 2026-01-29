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
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using GroupeV.Utilities;
using System.Windows.Media.Animation;

namespace GroupeV
{
    /// <summary>
    /// Logique d'interaction pour MainWindow.xaml
    /// Tableau de bord principal avec fonctionnalites de connexion a la base de donnees
    /// </summary>
    public partial class MainWindow : Window
    {
        private List<Produit> _productsData = [];
        private readonly Random _random = new();

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
        }

        /// <summary>
        /// Animation au chargement de la fenetre
        /// </summary>
        private void MainWindow_LoadedAnimation(object sender, RoutedEventArgs e)
        {
            // Creer des particules animees
            CreateAnimatedParticles();
            
            // Animer les panneaux de statistiques
            AnimateStatsPanels();
        }

        /// <summary>
        /// Creer des particules animees en arriere-plan
        /// </summary>
        private void CreateAnimatedParticles()
        {
            for (int i = 0; i < 20; i++)
            {
                var particle = new Ellipse
                {
                    Width = _random.Next(3, 8),
                    Height = _random.Next(3, 8),
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

                // Animation de deplacement
                var moveY = new DoubleAnimation
                {
                    From = Canvas.GetTop(particle),
                    To = _random.Next(0, (int)this.Height),
                    Duration = TimeSpan.FromSeconds(_random.Next(8, 15)),
                    RepeatBehavior = RepeatBehavior.Forever,
                    AutoReverse = true
                };

                var moveX = new DoubleAnimation
                {
                    From = Canvas.GetLeft(particle),
                    To = _random.Next(0, (int)this.Width),
                    Duration = TimeSpan.FromSeconds(_random.Next(8, 15)),
                    RepeatBehavior = RepeatBehavior.Forever,
                    AutoReverse = true
                };

                particle.BeginAnimation(Canvas.TopProperty, moveY);
                particle.BeginAnimation(Canvas.LeftProperty, moveX);
            }
        }

        /// <summary>
        /// Animer les panneaux de statistiques au chargement
        /// </summary>
        private async void AnimateStatsPanels()
        {
            var panels = new[] { Panel1, Panel2, Panel3, Panel4 };
            
            foreach (var panel in panels)
            {
                panel.Opacity = 0;
                panel.RenderTransform = new TranslateTransform { Y = 30 };
            }

            for (int i = 0; i < panels.Length; i++)
            {
                await System.Threading.Tasks.Task.Delay(100);
                
                var fadeIn = new DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    Duration = TimeSpan.FromSeconds(0.5)
                };

                var slideUp = new DoubleAnimation
                {
                    From = 30,
                    To = 0,
                    Duration = TimeSpan.FromSeconds(0.5),
                    EasingFunction = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.3 }
                };

                panels[i].BeginAnimation(OpacityProperty, fadeIn);
                if (panels[i].RenderTransform is TranslateTransform transform)
                {
                    transform.BeginAnimation(TranslateTransform.YProperty, slideUp);
                }
            }
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Verifier si l'utilisateur est authentifie
            if (!AuthenticationService.IsAuthenticated)
            {
                MessageBox.Show(this,
                    "Vous devez être connecté pour accéder à cette fenêtre.",
                    "Authentification requise",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                
                var loginWindow = new LoginWindow();
                loginWindow.Show();
                this.Close();
                return;
            }

            // Mettre a jour le message de bienvenue avec l'utilisateur actuel
            if (AuthenticationService.CurrentUser != null)
            {
                WelcomeTextBlock.Text = $"[SYSTEME] Bienvenue, {AuthenticationService.CurrentUser.Prenom} !";
            }

            await LoadDashboardDataAsync();
        }

        private async System.Threading.Tasks.Task LoadDashboardDataAsync()
        {
            try
            {
                StatusTextBlock.Text = "[STATUT] Chargement des donnees vendeur...";
                QuickStatsTextBlock.Text = "Base de donnees: vente_groupe\nStatut: Connexion...\nServeur: localhost:3306";

                using var context = new DatabaseContext();

                // Verifier d'abord la connexion
                bool canConnect = await context.Database.CanConnectAsync();
                if (!canConnect)
                {
                    StatusTextBlock.Text = "[ERREUR] Impossible de se connecter a la base de donnees";
                    QuickStatsTextBlock.Text = "Base de donnees: vente_groupe\nStatut: [X] HORS LIGNE\nServeur: localhost:3306";
                    MessageBox.Show(this,
                        "Impossible de se connecter a la base de donnees.\n\n" +
                        "Veuillez vous assurer que:\n" +
                        "1. Laragon MySQL est en cours d'execution\n" +
                        "2. La base de donnees 'vente_groupe' existe\n" +
                        "3. Le fichier SQL a ete importe\n\n" +
                        "Cliquez sur 'TESTER CONNEXION' pour les diagnostics.",
                        "Erreur de connexion a la base de donnees",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                // Charger les produits avec les informations du vendeur
                _productsData = await context.Produits
                    .Include(p => p.Vendeur)
                    .ThenInclude(v => v!.Utilisateur)
                    .Include(p => p.Categorie)
                    .OrderByDescending(p => p.CreatedAt)
                    .Take(50)
                    .ToListAsync();

                // Obtenir le premier vendeur comme vendeur actif (ou utiliser le vendeur connecte)
                var currentSeller = AuthenticationService.CurrentSeller;
                
                if (currentSeller != null && currentSeller.Utilisateur != null)
                {
                    ActiveSellerTextBlock.Text = $"{currentSeller.Utilisateur.Prenom} {currentSeller.Utilisateur.Nom}".ToUpper();
                    SellerEmailTextBlock.Text = currentSeller.EmailPro ?? currentSeller.Utilisateur.Email ?? "N/A";
                }
                else
                {
                    // Solution de repli vers le premier vendeur si non authentifie
                    var firstSeller = await context.Vendeurs
                        .Include(v => v.Utilisateur)
                        .FirstOrDefaultAsync();
                    
                    if (firstSeller != null && firstSeller.Utilisateur != null)
                    {
                        ActiveSellerTextBlock.Text = $"{firstSeller.Utilisateur.Prenom} {firstSeller.Utilisateur.Nom}".ToUpper();
                        SellerEmailTextBlock.Text = firstSeller.EmailPro ?? firstSeller.Utilisateur.Email ?? "N/A";
                    }
                }

                // Calculer les statistiques
                var totalProducts = _productsData.Count;
                var totalCategories = await context.Categories.CountAsync();
                var totalSellers = await context.Vendeurs.CountAsync();
                var totalPreventes = await context.Preventes.CountAsync();

                // Mettre a jour l'interface
                TotalSalesTextBlock.Text = totalProducts.ToString();
                RevenueTextBlock.Text = $"{totalCategories} Categories";
                ProductsSoldTextBlock.Text = totalSellers.ToString();

                // Remplir la DataGrid avec les produits
                SalesDataGrid.ItemsSource = _productsData;

                RecordCountTextBlock.Text = $"[ENREGISTREMENTS] {totalProducts} produits charges";
                StatusTextBlock.Text = "[STATUT] [OK] Systeme pret - Toutes les donnees chargees avec succes";
                QuickStatsTextBlock.Text = $"Base de donnees: vente_groupe\nStatut: [OK] CONNECTE\nServeur: localhost:3306\n\nProduits: {totalProducts}\nVendeurs: {totalSellers}\nCategories: {totalCategories}";
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = $"[ERREUR] Echec du chargement des donnees: {ex.Message}";
                QuickStatsTextBlock.Text = "Base de donnees: vente_groupe\nStatut: [X] ERREUR\nServeur: localhost:3306";
                MessageBox.Show(this, 
                    $"Erreur lors du chargement des donnees du tableau de bord:\n\n{ex.Message}\n\nVeuillez vous assurer que la base de donnees est correctement configuree.",
                    "Erreur de base de donnees",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        // ========== BOUTONS DE CONNEXION A LA BASE DE DONNEES ==========

        private async void TestConnectionButton_Click(object sender, RoutedEventArgs e)
        {
            StatusTextBlock.Text = "[STATUT] Test de la connexion a la base de donnees...";
            
            var button = sender as Button;
            if (button != null) button.IsEnabled = false;

            try
            {
                bool success = await DatabaseTester.TesterConnexionAsync(afficherMessageBox: true);
                
                if (success)
                {
                    StatusTextBlock.Text = "[STATUT] [OK] Test de connexion reussi !";
                    await LoadDashboardDataAsync(); // Recharger les donnees apres un test reussi
                }
                else
                {
                    StatusTextBlock.Text = "[STATUT] [X] Echec du test de connexion";
                }
            }
            finally
            {
                if (button != null) button.IsEnabled = true;
            }
        }

        private async void ShowDiagnosticsButton_Click(object sender, RoutedEventArgs e)
        {
            StatusTextBlock.Text = "[STATUT] Execution des diagnostics...";
            await DatabaseTester.AfficherDiagnosticAsync();
            StatusTextBlock.Text = "[STATUT] Diagnostics termines";
        }

        private void HealthCheckButton_Click(object sender, RoutedEventArgs e)
        {
            var healthWindow = new DatabaseHealthWindow();
            healthWindow.Owner = this;
            healthWindow.ShowDialog();
        }

        // ========== BOUTONS D'OPERATION SUR LES DONNEES ==========

        private async void AddProductButton_Click(object sender, RoutedEventArgs e)
        {
            var editWindow = new EditProductWindow();
            editWindow.Owner = this;
            var result = editWindow.ShowDialog();

            if (result == true)
            {
                StatusTextBlock.Text = "[STATUT] Produit ajoute avec succes";
                await LoadDashboardDataAsync(); // Recharger les donnees
            }
        }

        private async void EditProductButton_Click(object sender, RoutedEventArgs e)
        {
            if (SalesDataGrid.SelectedItem is not Produit selectedProduct)
            {
                MessageBox.Show(this,
                    "Veuillez selectionner un produit a modifier.",
                    "Aucun produit selectionne",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            var editWindow = new EditProductWindow(selectedProduct);
            editWindow.Owner = this;
            var result = editWindow.ShowDialog();

            if (result == true)
            {
                StatusTextBlock.Text = "[STATUT] Produit modifie avec succes";
                await LoadDashboardDataAsync(); // Recharger les donnees
            }
        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            StatusTextBlock.Text = "[STATUT] Actualisation des donnees...";
            await LoadDashboardDataAsync();
            StatusTextBlock.Text = "[STATUT] [OK] Donnees actualisees !";
        }

        private async void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            StatusTextBlock.Text = "[STATUT] Exportation des donnees...";
            
            try
            {
                var saveDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "Fichiers CSV (*.csv)|*.csv|Fichiers texte (*.txt)|*.txt|Tous les fichiers (*.*)|*.*",
                    FileName = $"export_produits_{DateTime.Now:yyyyMMdd_HHmmss}.csv",
                    Title = "Exporter les donnees des produits"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    var csvContent = new System.Text.StringBuilder();
                    csvContent.AppendLine("ID;Produit;Prix;Categorie;Vendeur;Date Creation");

                    foreach (var product in _productsData)
                    {
                        csvContent.AppendLine($"{product.IdProduit};{product.Description};{product.Prix};{product.CategorieNom};{product.VendeurNom};{product.CreatedAt:dd/MM/yyyy}");
                    }

                    await System.IO.File.WriteAllTextAsync(saveDialog.FileName, csvContent.ToString(), System.Text.Encoding.UTF8);

                    StatusTextBlock.Text = $"[STATUT] [OK] Donnees exportees : {saveDialog.FileName}";
                    MessageBox.Show(this,
                        $"Exportation reussie !\n\n{_productsData.Count} produits exportes vers:\n{saveDialog.FileName}",
                        "Exportation terminee",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                else
                {
                    StatusTextBlock.Text = "[STATUT] Exportation annulee";
                }
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = "[STATUT] [X] Erreur lors de l'exportation";
                MessageBox.Show(this,
                    $"Erreur lors de l'exportation des donnees:\n\n{ex.Message}",
                    "Erreur d'exportation",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(this,
                "Etes-vous sur de vouloir vous deconnecter ?",
                "Confirmation de deconnexion",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                AuthenticationService.Logout();
                StatusTextBlock.Text = "[STATUT] Deconnexion...";

                var loginWindow = new LoginWindow();
                loginWindow.Show();
                this.Close();
            }
        }
    }
}