using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using GroupeV.Controls;
using GroupeV.Utilities;
using GroupeV.ViewModels;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace GroupeV
{
    /// <summary>
    /// Logique d'interaction pour MainWindow.xaml
    /// Tableau de bord Neumorphic avec MVVM — le ViewModel gère l'état,
    /// le code-behind ne gère que l'init, les graphiques LiveCharts et les dialogues.
    /// </summary>
    public partial class MainWindow : Window
    {
        private DashboardViewModel ViewModel => (DashboardViewModel)DataContext;

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (!AuthenticationService.IsAuthenticated)
            {
                NeuDialog.ShowWarning(this, "Authentification requise",
                    "Vous devez être connecté pour accéder à cette fenêtre.");

                var loginWindow = new LoginWindow();
                loginWindow.Show();
                Close();
                return;
            }

            await ViewModel.LoadDashboardDataAsync();
            LoadChartsAndAnalytics();
            LoadActivityHeatmap();
        }

        // ========== GRAPHIQUES ==========

        private void LoadChartsAndAnalytics()
        {
            var products = ViewModel.Produits;
            if (products.Count == 0) return;

            try
            {
                // Couleur d'accent neumorphic pour les charts
                var accentColor = SKColor.Parse("#6366F1");
                var accentColor2 = SKColor.Parse("#F59E0B");
                var labelColor = SKColor.Parse("#64748B");

                // 1. Camembert catégories
                var categoriesGrouped = products
                    .GroupBy(p => p.CategorieNom ?? "Non catégorisé")
                    .Select(g => new { Category = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count)
                    .ToList();

                CategoriesPieChart.Series = [.. categoriesGrouped.Select(c => new PieSeries<int>
                {
                    Values = [c.Count],
                    Name = $"{c.Category} ({c.Count})",
                    DataLabelsPaint = new SolidColorPaint(SKColors.White),
                    DataLabelsSize = 11,
                    DataLabelsPosition = LiveChartsCore.Measure.PolarLabelsPosition.Middle
                })];

                // 2. Prix moyens par catégorie
                var prixMoyens = products
                    .Where(p => p.Prix.HasValue)
                    .GroupBy(p => p.CategorieNom ?? "Non catégorisé")
                    .Select(g => new { Category = g.Key, AvgPrice = g.Average(p => (double)p.Prix!.Value) })
                    .OrderByDescending(x => x.AvgPrice)
                    .Take(10)
                    .ToList();

                PrixMoyensChart.Series =
                [
                    new ColumnSeries<double>
                    {
                        Values = [.. prixMoyens.Select(c => c.AvgPrice)],
                        Name = "Prix Moyen (€)",
                        Fill = new SolidColorPaint(accentColor2),
                        DataLabelsPaint = new SolidColorPaint(labelColor),
                        DataLabelsSize = 10,
                        DataLabelsFormatter = point => $"{point.Coordinate.PrimaryValue:F2} €"
                    }
                ];
                PrixMoyensChart.XAxes =
                [
                    new Axis
                    {
                        Labels = [.. prixMoyens.Select(c => c.Category.Length > 12 ? c.Category[..9] + "..." : c.Category)],
                        LabelsRotation = 45,
                        TextSize = 10,
                        LabelsPaint = new SolidColorPaint(labelColor)
                    }
                ];
                PrixMoyensChart.YAxes =
                [
                    new Axis
                    {
                        TextSize = 10,
                        LabelsPaint = new SolidColorPaint(labelColor),
                        Labeler = value => $"{value:F0} €"
                    }
                ];
            }
            catch (Exception ex)
            {
                ViewModel.StatusMessage = $"Impossible de charger les graphiques: {ex.Message}";
            }
        }

        private void LoadActivityHeatmap()
        {
            ActivityHeatmapPanel.Children.Clear();
            var products = ViewModel.Produits;

            var last7Days = Enumerable.Range(0, 7)
                .Select(i => DateTime.Now.AddDays(-i))
                .Reverse()
                .ToList();

            foreach (var day in last7Days)
            {
                int count = products.Count(p => p.CreatedAt.Date == day.Date);
                double intensity = count > 0 ? Math.Min(count / 5.0, 1.0) : 0;

                var dayBorder = new Border
                {
                    Background = new SolidColorBrush(GetHeatmapColor(intensity)),
                    Padding = new Thickness(10, 6, 10, 6),
                    Margin = new Thickness(0, 0, 0, 4),
                    CornerRadius = new CornerRadius(10)
                };

                var stack = new StackPanel { Orientation = Orientation.Horizontal };
                stack.Children.Add(new TextBlock
                {
                    Text = day.ToString("ddd dd/MM"),
                    FontSize = 11,
                    Foreground = FindResource("NeuTextSecondaryBrush") as SolidColorBrush,
                    Width = 90
                });
                stack.Children.Add(new TextBlock
                {
                    Text = $"{count} produit(s)",
                    FontSize = 11,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = FindResource("NeuTextPrimaryBrush") as SolidColorBrush
                });

                dayBorder.Child = stack;
                ActivityHeatmapPanel.Children.Add(dayBorder);
            }
        }

        private static Color GetHeatmapColor(double intensity)
        {
            if (intensity == 0) return Color.FromRgb(0xE2, 0xE8, 0xF0);
            if (intensity < 0.2) return Color.FromRgb(0xC7, 0xD2, 0xFE);
            if (intensity < 0.4) return Color.FromRgb(0xA5, 0xB4, 0xFC);
            if (intensity < 0.6) return Color.FromRgb(0x81, 0x8C, 0xF8);
            if (intensity < 0.8) return Color.FromRgb(0x63, 0x66, 0xF1);
            return Color.FromRgb(0x4F, 0x46, 0xE5);
        }

        // ========== EVENT HANDLERS (délèguent au ViewModel ou ouvrent des dialogues) ==========

        private async void TestConnectionButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn) btn.IsEnabled = false;
            try
            {
                bool success = await DatabaseTester.TesterConnexionAsync(afficherMessageBox: true);
                if (success)
                {
                    await ViewModel.LoadDashboardDataAsync();
                    LoadChartsAndAnalytics();
                    LoadActivityHeatmap();
                }
            }
            finally
            {
                if (sender is Button btn2) btn2.IsEnabled = true;
            }
        }

        private async void ShowDiagnosticsButton_Click(object sender, RoutedEventArgs e)
        {
            await DatabaseTester.AfficherDiagnosticAsync();
        }

        private void HealthCheckButton_Click(object sender, RoutedEventArgs e)
        {
            var healthWindow = new DatabaseHealthWindow { Owner = this };
            healthWindow.ShowDialog();
        }

        private async void AddProductButton_Click(object sender, RoutedEventArgs e)
        {
            var editWindow = new EditProductWindow { Owner = this };
            if (editWindow.ShowDialog() == true)
            {
                await ViewModel.LoadDashboardDataAsync();
                LoadChartsAndAnalytics();
                LoadActivityHeatmap();
            }
        }

        private async void EditProductButton_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.SelectedProduct is not { } selected)
            {
                NeuDialog.ShowInfo(this, "Aucun produit sélectionné",
                    "Veuillez sélectionner un produit à modifier.");
                return;
            }

            // Vérification de propriété : le vendeur ne peut modifier que ses propres produits
            if (AuthenticationService.CurrentSeller != null &&
                selected.IdVendeur != AuthenticationService.CurrentSeller.IdUser)
            {
                NeuDialog.ShowWarning(this, "Accès refusé",
                    "Vous ne pouvez modifier que vos propres produits.");
                return;
            }

            var editWindow = new EditProductWindow(selected) { Owner = this };
            if (editWindow.ShowDialog() == true)
            {
                await ViewModel.LoadDashboardDataAsync();
                LoadChartsAndAnalytics();
                LoadActivityHeatmap();
            }
        }

        private async void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var saveDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "Fichiers CSV (*.csv)|*.csv|Tous les fichiers (*.*)|*.*",
                    FileName = $"export_produits_{DateTime.Now:yyyyMMdd_HHmmss}.csv",
                    Title = "Exporter les données des produits"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    var csv = new System.Text.StringBuilder();
                    csv.AppendLine("ID;Produit;Prix;Categorie;Vendeur;Date Creation");
                    foreach (var p in ViewModel.Produits)
                        csv.AppendLine($"{p.IdProduit};{p.Description};{p.Prix};{p.CategorieNom};{p.VendeurNom};{p.CreatedAt:dd/MM/yyyy}");

                    await System.IO.File.WriteAllTextAsync(saveDialog.FileName, csv.ToString(), System.Text.Encoding.UTF8);
                    NeuDialog.ShowSuccess(this, "Exportation terminée",
                        $"Exportation réussie !\n\n{ViewModel.Produits.Count} produits exportés.");
                }
            }
            catch (Exception ex)
            {
                NeuDialog.ShowError(this, "Erreur d'exportation",
                    $"Erreur lors de l'exportation:\n\n{ex.Message}");
            }
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            if (NeuDialog.Confirm(this, "Confirmation",
                "Êtes-vous sûr de vouloir vous déconnecter ?"))
            {
                AuthenticationService.Logout();
                var loginWindow = new LoginWindow();
                loginWindow.Show();
                Close();
            }
        }

        // ========== TAB SWITCHING ==========

        private void TabProduits_Click(object sender, RoutedEventArgs e)
        {
            ProduitsTab.Visibility = Visibility.Visible;
            FacturesTab.Visibility = Visibility.Collapsed;
            TabProduitsBtn.Style = (Style)FindResource("NeuButtonAccent");
            TabFacturesBtn.Style = (Style)FindResource("NeuButton");
        }

        private void TabFactures_Click(object sender, RoutedEventArgs e)
        {
            ProduitsTab.Visibility = Visibility.Collapsed;
            FacturesTab.Visibility = Visibility.Visible;
            TabFacturesBtn.Style = (Style)FindResource("NeuButtonAccent");
            TabProduitsBtn.Style = (Style)FindResource("NeuButton");
        }

        // ========== PANEL TOGGLES ==========

        private void ToggleLeft_Click(object sender, RoutedEventArgs e)
        {
            bool isVisible = LeftNavBorder.Visibility == Visibility.Visible;
            LeftNavBorder.Visibility = isVisible ? Visibility.Collapsed : Visibility.Visible;
            LeftColDef.Width = isVisible ? new GridLength(0) : new GridLength(220);
            ToggleLeftBtn.Content = isVisible ? "▶" : "◀";
        }

        private void ToggleRight_Click(object sender, RoutedEventArgs e)
        {
            bool isVisible = RightChartsScrollViewer.Visibility == Visibility.Visible;
            RightChartsScrollViewer.Visibility = isVisible ? Visibility.Collapsed : Visibility.Visible;
            RightColDef.Width = isVisible ? new GridLength(0) : new GridLength(300);
            ToggleRightBtn.Content = isVisible ? "◀" : "▶";
        }

        // ========== PROFILE & TICKETS ==========

        private void EditProfileButton_Click(object sender, RoutedEventArgs e)
        {
            var profileWindow = new EditProfileWindow { Owner = this };
            if (profileWindow.ShowDialog() == true)
                ViewModel.WelcomeMessage = AuthenticationService.CurrentUser?.NomComplet ?? "Bienvenue";
        }

        private void TicketsButton_Click(object sender, RoutedEventArgs e)
        {
            var ticketWindow = new TicketWindow { Owner = this };
            ticketWindow.ShowDialog();
        }
    }
}