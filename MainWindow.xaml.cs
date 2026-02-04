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
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

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

                // Charger les graphiques et analytics
                await LoadChartsAndAnalyticsAsync();
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

        // ========== GRAPHIQUES ET ANALYTICS ==========

        private async System.Threading.Tasks.Task LoadChartsAndAnalyticsAsync()
        {
            try
            {
                if (_productsData == null || !_productsData.Any())
                    return;

                // 1. Graphique Camembert des Categories
                var categoriesGrouped = _productsData
                    .GroupBy(p => p.CategorieNom ?? "Non catégorisé")
                    .Select(g => new { Category = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count)
                    .ToList();

                var pieSeries = categoriesGrouped.Select(c => new PieSeries<int>
                {
                    Values = new[] { c.Count },
                    Name = $"{c.Category} ({c.Count})",
                    DataLabelsPaint = new SolidColorPaint(SKColors.White),
                    DataLabelsSize = 12,
                    DataLabelsPosition = LiveChartsCore.Measure.PolarLabelsPosition.Middle
                }).ToArray();

                CategoriesPieChart.Series = pieSeries;

                // 2. Graphique Barres des Vendeurs
                var vendeursGrouped = _productsData
                    .GroupBy(p => p.VendeurNom ?? "Inconnu")
                    .Select(g => new { Vendeur = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count)
                    .Take(10)
                    .ToList();

                var columnSeries = new ColumnSeries<int>
                {
                    Values = vendeursGrouped.Select(v => v.Count).ToArray(),
                    Name = "Produits",
                    Fill = new SolidColorPaint(SKColor.Parse("#00FF00")),
                    DataLabelsPaint = new SolidColorPaint(SKColors.White),
                    DataLabelsSize = 10,
                    DataLabelsPosition = LiveChartsCore.Measure.DataLabelsPosition.End
                };

                VendeursBarChart.Series = new ISeries[] { columnSeries };
                VendeursBarChart.XAxes = new[]
                {
                    new Axis
                    {
                        Labels = vendeursGrouped.Select(v => v.Vendeur.Length > 15 ? v.Vendeur.Substring(0, 12) + "..." : v.Vendeur).ToArray(),
                        LabelsRotation = 45,
                        TextSize = 10,
                        LabelsPaint = new SolidColorPaint(SKColor.Parse("#00CC00"))
                    }
                };
                VendeursBarChart.YAxes = new[]
                {
                    new Axis
                    {
                        TextSize = 10,
                        LabelsPaint = new SolidColorPaint(SKColor.Parse("#00CC00"))
                    }
                };

                // 3. Graphique Prix Moyens par Categorie
                var prixMoyensGrouped = _productsData
                    .Where(p => p.Prix.HasValue)
                    .GroupBy(p => p.CategorieNom ?? "Non catégorisé")
                    .Select(g => new { 
                        Category = g.Key, 
                        AvgPrice = g.Average(p => (double)p.Prix!.Value) 
                    })
                    .OrderByDescending(x => x.AvgPrice)
                    .Take(10)
                    .ToList();

                var barSeries = new ColumnSeries<double>
                {
                    Values = prixMoyensGrouped.Select(c => c.AvgPrice).ToArray(),
                    Name = "Prix Moyen (€)",
                    Fill = new SolidColorPaint(SKColor.Parse("#FFD700")),
                    DataLabelsPaint = new SolidColorPaint(SKColors.White),
                    DataLabelsSize = 10,
                    DataLabelsFormatter = point => $"{point.PrimaryValue:F2}€"
                };

                PrixMoyensChart.Series = new ISeries[] { barSeries };
                PrixMoyensChart.XAxes = new[]
                {
                    new Axis
                    {
                        Labels = prixMoyensGrouped.Select(c => c.Category.Length > 12 ? c.Category.Substring(0, 9) + "..." : c.Category).ToArray(),
                        LabelsRotation = 45,
                        TextSize = 10,
                        LabelsPaint = new SolidColorPaint(SKColor.Parse("#00CC00"))
                    }
                };
                PrixMoyensChart.YAxes = new[]
                {
                    new Axis
                    {
                        TextSize = 10,
                        LabelsPaint = new SolidColorPaint(SKColor.Parse("#00CC00")),
                        Labeler = value => $"{value:F0}€"
                    }
                };

                // 4. Analytics Avancés
                var productsWithPrice = _productsData.Where(p => p.Prix.HasValue).ToList();
                
                if (productsWithPrice.Any())
                {
                    var mostExpensive = productsWithPrice.OrderByDescending(p => p.Prix).First();
                    MostExpensiveProductTextBlock.Text = mostExpensive.Description ?? "N/A";
                    MostExpensivePriceTextBlock.Text = $"{mostExpensive.Prix:F2}€";

                    var leastExpensive = productsWithPrice.OrderBy(p => p.Prix).First();
                    LeastExpensiveProductTextBlock.Text = leastExpensive.Description ?? "N/A";
                    LeastExpensivePriceTextBlock.Text = $"{leastExpensive.Prix:F2}€";

                    var averagePrice = productsWithPrice.Average(p => (double)p.Prix!.Value);
                    AveragePriceTextBlock.Text = $"{averagePrice:F2}€";

                    // Animation pour le prix moyen
                    AnimateNumberTextBlock(AveragePriceTextBlock, 0, averagePrice, TimeSpan.FromSeconds(2));
                }

                // 5. Heatmap d'activité (derniers 7 jours)
                LoadActivityHeatmap();

                await System.Threading.Tasks.Task.CompletedTask;
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = $"[ERREUR] Impossible de charger les graphiques: {ex.Message}";
            }
        }

        private void LoadActivityHeatmap()
        {
            ActivityHeatmapPanel.Children.Clear();

            var last7Days = Enumerable.Range(0, 7)
                .Select(i => DateTime.Now.AddDays(-i))
                .Reverse()
                .ToList();

            foreach (var day in last7Days)
            {
                var dayProducts = _productsData.Count(p => p.CreatedAt.Date == day.Date);
                
                var intensity = dayProducts > 0 ? Math.Min(dayProducts / 5.0, 1.0) : 0;
                var color = GetHeatmapColor(intensity);

                var dayBorder = new Border
                {
                    Background = new SolidColorBrush(color),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(0, 204, 0)),
                    BorderThickness = new Thickness(1),
                    Padding = new Thickness(8, 5, 8, 5),
                    Margin = new Thickness(0, 0, 0, 5),
                    CornerRadius = new CornerRadius(3)
                };

                var stackPanel = new StackPanel { Orientation = Orientation.Horizontal };
                
                var dateText = new TextBlock
                {
                    Text = day.ToString("ddd dd/MM"),
                    FontFamily = new FontFamily("Consolas"),
                    FontSize = 10,
                    Foreground = new SolidColorBrush(Colors.LimeGreen),
                    Width = 80
                };

                var countText = new TextBlock
                {
                    Text = $"{dayProducts} produit(s)",
                    FontFamily = new FontFamily("Consolas"),
                    FontSize = 10,
                    Foreground = new SolidColorBrush(Colors.White),
                    FontWeight = FontWeights.Bold
                };

                stackPanel.Children.Add(dateText);
                stackPanel.Children.Add(countText);
                dayBorder.Child = stackPanel;

                ActivityHeatmapPanel.Children.Add(dayBorder);
            }
        }

        private Color GetHeatmapColor(double intensity)
        {
            if (intensity == 0) return Color.FromRgb(10, 10, 10);
            if (intensity < 0.2) return Color.FromRgb(0, 50, 0);
            if (intensity < 0.4) return Color.FromRgb(0, 100, 0);
            if (intensity < 0.6) return Color.FromRgb(0, 150, 0);
            if (intensity < 0.8) return Color.FromRgb(0, 200, 0);
            return Color.FromRgb(0, 255, 0);
        }

        private void AnimateNumberTextBlock(TextBlock textBlock, double from, double to, TimeSpan duration)
        {
            var animation = new DoubleAnimation
            {
                From = from,
                To = to,
                Duration = duration,
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };

            var animationClock = animation.CreateClock();
            animationClock.CurrentTimeInvalidated += (s, e) =>
            {
                if (animationClock.CurrentTime.HasValue)
                {
                    var progress = animationClock.CurrentTime.Value.TotalSeconds / duration.TotalSeconds;
                    var currentValue = from + (to - from) * progress;
                    textBlock.Text = $"{currentValue:F2}€";
                }
            };
            animationClock.Controller?.Begin();
        }
    }
}