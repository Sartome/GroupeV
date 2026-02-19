using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;

namespace GroupeV.ViewModels;

/// <summary>
/// ViewModel principal du tableau de bord.
/// Pattern MVVM strict : aucune dépendance directe vers la Vue.
/// </summary>
internal sealed class DashboardViewModel : ViewModelBase
{
    // ========== COLLECTIONS ==========

    public ObservableCollection<Produit> Produits { get; } = [];
    public ObservableCollection<Produit> FilteredProduits { get; } = [];
    public ObservableCollection<InvoiceItem> Factures { get; } = [];

    // ========== PROPRIÉTÉS LIÉES ==========

    private string _welcomeMessage = "Bienvenue";
    public string WelcomeMessage
    {
        get => _welcomeMessage;
        set => SetProperty(ref _welcomeMessage, value);
    }

    private string _statusMessage = "Système prêt";
    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    private string _recordCount = "0 produits chargés";
    public string RecordCount
    {
        get => _recordCount;
        set => SetProperty(ref _recordCount, value);
    }

    private int _totalProduits;
    public int TotalProduits
    {
        get => _totalProduits;
        set => SetProperty(ref _totalProduits, value);
    }

    private int _totalCategories;
    public int TotalCategories
    {
        get => _totalCategories;
        set => SetProperty(ref _totalCategories, value);
    }

    private int _totalVendeurs;
    public int TotalVendeurs
    {
        get => _totalVendeurs;
        set => SetProperty(ref _totalVendeurs, value);
    }

    private int _totalStock;
    public int TotalStock
    {
        get => _totalStock;
        set => SetProperty(ref _totalStock, value);
    }

    private string _activeSellerName = "CHARGEMENT...";
    public string ActiveSellerName
    {
        get => _activeSellerName;
        set => SetProperty(ref _activeSellerName, value);
    }

    private string _activeSellerEmail = "initialisation...";
    public string ActiveSellerEmail
    {
        get => _activeSellerEmail;
        set => SetProperty(ref _activeSellerEmail, value);
    }

    private string _quickStats = "Base de données: vente_groupe\nStatut: Connexion...\nServeur: localhost:3306";
    public string QuickStats
    {
        get => _quickStats;
        set => SetProperty(ref _quickStats, value);
    }

    private string _mostExpensiveProduct = "--";
    public string MostExpensiveProduct
    {
        get => _mostExpensiveProduct;
        set => SetProperty(ref _mostExpensiveProduct, value);
    }

    private string _mostExpensivePrice = "0,00 €";
    public string MostExpensivePrice
    {
        get => _mostExpensivePrice;
        set => SetProperty(ref _mostExpensivePrice, value);
    }

    private string _leastExpensiveProduct = "--";
    public string LeastExpensiveProduct
    {
        get => _leastExpensiveProduct;
        set => SetProperty(ref _leastExpensiveProduct, value);
    }

    private string _leastExpensivePrice = "0,00 €";
    public string LeastExpensivePrice
    {
        get => _leastExpensivePrice;
        set => SetProperty(ref _leastExpensivePrice, value);
    }

    private string _averagePrice = "0,00 €";
    public string AveragePrice
    {
        get => _averagePrice;
        set => SetProperty(ref _averagePrice, value);
    }

    private bool _isLoading;
    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    private Produit? _selectedProduct;
    public Produit? SelectedProduct
    {
        get => _selectedProduct;
        set => SetProperty(ref _selectedProduct, value);
    }

    // Toast notification
    private string _toastMessage = string.Empty;
    public string ToastMessage
    {
        get => _toastMessage;
        set => SetProperty(ref _toastMessage, value);
    }

    private bool _isToastVisible;
    public bool IsToastVisible
    {
        get => _isToastVisible;
        set => SetProperty(ref _isToastVisible, value);
    }

    private bool _isToastError;
    public bool IsToastError
    {
        get => _isToastError;
        set => SetProperty(ref _isToastError, value);
    }

    private string _searchText = string.Empty;
    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
                ApplyFilter();
        }
    }

    // ========== COMMANDS ==========

    public ICommand LoadDataCommand { get; }
    public ICommand TestConnectionCommand { get; }
    public ICommand LogoutCommand { get; }

    // ========== CONSTRUCTEUR ==========

    public DashboardViewModel()
    {
        LoadDataCommand = new AsyncRelayCommand(LoadDashboardDataAsync);
        TestConnectionCommand = new AsyncRelayCommand(TestConnectionAsync);
        LogoutCommand = new RelayCommand(Logout);
    }

    // ========== MÉTHODES ASYNC ==========

    /// <summary>
    /// Charge toutes les données du tableau de bord de manière asynchrone.
    /// </summary>
    public async Task LoadDashboardDataAsync()
    {
        IsLoading = true;
        StatusMessage = "Chargement des données vendeur...";
        QuickStats = "Base de données: vente_groupe\nStatut: Connexion...\nServeur: localhost:3306";

        try
        {
            using var context = new DatabaseContext();

            bool canConnect = await context.Database.CanConnectAsync();
            if (!canConnect)
            {
                StatusMessage = "Impossible de se connecter à la base de données";
                QuickStats = "Base de données: vente_groupe\nStatut: HORS LIGNE\nServeur: localhost:3306";
                ShowToast("Impossible de se connecter à la base de données. Vérifiez que MySQL est en cours d'exécution.", isError: true);
                return;
            }

            // Le vendeur ne voit que SES produits
            var currentSellerId = AuthenticationService.CurrentSeller?.IdUser;

            var query = context.Produits
                .Include(p => p.Vendeur)
                .ThenInclude(v => v!.Utilisateur)
                .Include(p => p.Categorie)
                .AsQueryable();

            if (currentSellerId.HasValue)
            {
                query = query.Where(p => p.IdVendeur == currentSellerId.Value);
            }

            var products = await query
                .OrderByDescending(p => p.CreatedAt)
                .Take(100)
                .ToListAsync();

            Produits.Clear();
            foreach (var p in products)
                Produits.Add(p);

            // Vendeur actif
            var currentSeller = AuthenticationService.CurrentSeller;
            if (currentSeller?.Utilisateur != null)
            {
                ActiveSellerName = $"{currentSeller.Utilisateur.Prenom} {currentSeller.Utilisateur.Nom}".ToUpper();
                ActiveSellerEmail = currentSeller.EmailPro ?? currentSeller.Utilisateur.Email ?? "N/A";
            }

            // Statistiques
            TotalProduits = products.Count;
            TotalCategories = await context.Categories.CountAsync();
            TotalVendeurs = await context.Vendeurs.CountAsync();
            TotalStock = products.Sum(p => p.Quantity);

            // Analytics
            var withPrice = products.Where(p => p.Prix.HasValue).ToList();
            if (withPrice.Count > 0)
            {
                var most = withPrice.OrderByDescending(p => p.Prix).First();
                MostExpensiveProduct = most.Description ?? "N/A";
                MostExpensivePrice = $"{most.Prix:F2} €";

                var least = withPrice.OrderBy(p => p.Prix).First();
                LeastExpensiveProduct = least.Description ?? "N/A";
                LeastExpensivePrice = $"{least.Prix:F2} €";

                AveragePrice = $"{withPrice.Average(p => (double)p.Prix!.Value):F2} €";
            }

            RecordCount = $"{TotalProduits} produits · {TotalStock} articles en stock";
            StatusMessage = "Système prêt — Toutes les données chargées avec succès";
            QuickStats = $"Base de données: vente_groupe\nStatut: CONNECTÉ\nServeur: localhost:3306\n\nProduits: {TotalProduits}\nStock total: {TotalStock}\nVendeurs: {TotalVendeurs}\nCatégories: {TotalCategories}";

            if (AuthenticationService.CurrentUser != null)
            {
                WelcomeMessage = $"Bienvenue, {AuthenticationService.CurrentUser.Prenom} !";
            }

            ApplyFilter();
            await LoadInvoicesAsync(context);

            ShowToast("Données chargées avec succès", isError: false);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Échec du chargement des données: {ex.Message}";
            QuickStats = "Base de données: vente_groupe\nStatut: ERREUR\nServeur: localhost:3306";
            ShowToast($"Erreur: {ex.Message}", isError: true);
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Teste la connexion à la base de données.
    /// </summary>
    private async Task TestConnectionAsync()
    {
        StatusMessage = "Test de la connexion...";
        try
        {
            bool success = await Utilities.DatabaseTester.TesterConnexionAsync(afficherMessageBox: false);
            if (success)
            {
                ShowToast("Connexion réussie !", isError: false);
                await LoadDashboardDataAsync();
            }
            else
            {
                ShowToast("Échec de la connexion", isError: true);
                StatusMessage = "Échec du test de connexion";
            }
        }
        catch (Exception ex)
        {
            ShowToast($"Erreur: {ex.Message}", isError: true);
        }
    }

    private void Logout()
    {
        AuthenticationService.Logout();
        var loginWindow = new LoginWindow();
        loginWindow.Show();
        Application.Current.MainWindow?.Close();
    }

    /// <summary>
    /// Affiche un toast de notification non intrusif.
    /// </summary>
    private async void ShowToast(string message, bool isError)
    {
        ToastMessage = message;
        IsToastError = isError;
        IsToastVisible = true;

        await Task.Delay(3500);
        IsToastVisible = false;
    }

    /// <summary>
    /// Filtre les produits selon le texte de recherche.
    /// </summary>
    private void ApplyFilter()
    {
        FilteredProduits.Clear();
        var query = string.IsNullOrWhiteSpace(SearchText)
            ? Produits
            : Produits.Where(p =>
                (p.Description ?? "").Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                (p.CategorieNom ?? "").Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                (p.PrixFormate ?? "").Contains(SearchText, StringComparison.OrdinalIgnoreCase));

        foreach (var p in query)
            FilteredProduits.Add(p);
    }

    /// <summary>
    /// Charge les données de facturation par article du vendeur connecté.
    /// </summary>
    private async Task LoadInvoicesAsync(DatabaseContext context)
    {
        Factures.Clear();
        var currentSellerId = AuthenticationService.CurrentSeller?.IdUser;
        if (!currentSellerId.HasValue) return;

        var preventes = await context.Preventes
            .Include(pv => pv.Produit)
            .Where(pv => pv.Produit != null && pv.Produit.IdVendeur == currentSellerId.Value)
            .OrderByDescending(pv => pv.CreatedAt)
            .Take(50)
            .ToListAsync();

        foreach (var pv in preventes)
        {
            Factures.Add(new InvoiceItem
            {
                IdPrevente = pv.IdPrevente,
                ProduitDescription = pv.Produit?.Description ?? "N/A",
                PrixPrevente = pv.PrixPrevente,
                DateLimite = pv.DateLimite,
                Statut = pv.StatusFormate,
                NombreMin = pv.NombreMin,
                CreatedAt = pv.CreatedAt
            });
        }
    }
}
