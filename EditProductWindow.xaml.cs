using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using GroupeV.Controls;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;

namespace GroupeV
{
    /// <summary>
    /// Edit Product Window — create or edit products with dynamic sale-type sections.
    /// </summary>
    public partial class EditProductWindow : Window
    {
        private static readonly string[] AllowedImageExtensions = [".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp"];
        private const long MaxImageSizeBytes = 5 * 1024 * 1024;

        private readonly Produit? _product;
        private readonly bool _isEditMode;
        private string? _selectedImagePath;
        private int _selectedTypeVente; // 0=buy, 1=group, 2=auction
        private bool _isPriceUpdating;

        public EditProductWindow()
        {
            InitializeComponent();
            _isEditMode = false;
            HeaderTextBlock.Text = "Nouveau Produit";
            Loaded += EditProductWindow_Loaded;
        }

        public EditProductWindow(Produit product)
        {
            InitializeComponent();
            _product = product;
            _isEditMode = true;
            HeaderTextBlock.Text = "Modifier le Produit";
            DeleteButton.Visibility = Visibility.Visible;
            ProductIdBorder.Visibility = Visibility.Visible;
            Loaded += EditProductWindow_Loaded;
        }

        private async void EditProductWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (_isEditMode && _product != null && AuthenticationService.CurrentSeller != null)
            {
                if (_product.IdVendeur != AuthenticationService.CurrentSeller.IdUser)
                {
                    NeuDialog.ShowWarning(this, "Accès refusé", "Vous ne pouvez modifier que vos propres produits.");
                    Close();
                    return;
                }
            }

            await LoadCategoriesAsync();

            if (_isEditMode && _product != null)
                LoadProductData();
            else
                SelectTypeCard(0);
        }

        // ═══ TYPE CARD SELECTION ═══════════════════════════════════════════

        private void SelectTypeCard(int type)
        {
            _selectedTypeVente = type;

            var accent    = (System.Windows.Media.SolidColorBrush)FindResource("NeuAccentBrush");
            var divider   = (System.Windows.Media.SolidColorBrush)FindResource("NeuDividerBrush");
            var textPrimary = (System.Windows.Media.SolidColorBrush)FindResource("NeuTextPrimaryBrush");

            CardBuy.BorderBrush     = divider;
            CardAuction.BorderBrush = divider;
            CardGroup.BorderBrush   = divider;
            CardBuyTitle.Foreground     = textPrimary;
            CardAuctionTitle.Foreground = textPrimary;
            CardGroupTitle.Foreground   = textPrimary;

            switch (type)
            {
                case 0: CardBuy.BorderBrush = accent;     CardBuyTitle.Foreground = accent;     break;
                case 1: CardGroup.BorderBrush = accent;   CardGroupTitle.Foreground = accent;   break;
                case 2: CardAuction.BorderBrush = accent; CardAuctionTitle.Foreground = accent; break;
            }

            SectionBuy.Visibility     = type == 0 ? Visibility.Visible : Visibility.Collapsed;
            SectionGroup.Visibility   = type == 1 ? Visibility.Visible : Visibility.Collapsed;
            SectionAuction.Visibility = type == 2 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void CardBuy_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)     => SelectTypeCard(0);
        private void CardAuction_Click(object sender, System.Windows.Input.MouseButtonEventArgs e) => SelectTypeCard(2);
        private void CardGroup_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)   => SelectTypeCard(1);

        // ═══ PRICE AUTO-CALCULATION (HT ↔ TTC, TVA=20%) ════════════════════

        private void PrixHtBuy_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isPriceUpdating) return;
            if (!TryParseDecimal(PrixHtBuyBox.Text, out var ht)) return;
            _isPriceUpdating = true;
            PrixTtcBuyBox.Text = (ht * 1.20m).ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
            _isPriceUpdating = false;
        }

        private void PrixTtcBuy_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isPriceUpdating) return;
            if (!TryParseDecimal(PrixTtcBuyBox.Text, out var ttc)) return;
            _isPriceUpdating = true;
            PrixHtBuyBox.Text = (ttc / 1.20m).ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
            _isPriceUpdating = false;
        }

        private void PrixHtGroup_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isPriceUpdating) return;
            if (!TryParseDecimal(PrixHtGroupBox.Text, out var ht)) return;
            _isPriceUpdating = true;
            PrixTtcGroupBox.Text = (ht * 1.20m).ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
            _isPriceUpdating = false;
        }

        private void PrixTtcGroup_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isPriceUpdating) return;
            if (!TryParseDecimal(PrixTtcGroupBox.Text, out var ttc)) return;
            _isPriceUpdating = true;
            PrixHtGroupBox.Text = (ttc / 1.20m).ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
            _isPriceUpdating = false;
        }

        // ═══ LOAD DATA ══════════════════════════════════════════════════════

        private async System.Threading.Tasks.Task LoadCategoriesAsync()
        {
            try
            {
                using var context = new DatabaseContext();
                var categories = await context.Categories.OrderBy(c => c.Libelle).ToListAsync();
                CategoryComboBox.ItemsSource = categories;
                if (_product?.IdCategorie.HasValue == true)
                    CategoryComboBox.SelectedValue = _product.IdCategorie.Value;
            }
            catch (Exception ex)
            {
                ShowStatus($"Erreur lors du chargement des catégories: {ex.Message}", isError: true);
            }
        }

        private void LoadProductData()
        {
            if (_product == null) return;

            ProductIdTextBlock.Text = _product.IdProduit.ToString();
            DescriptionTextBox.Text = _product.Description ?? string.Empty;
            ImageUrlTextBox.Text    = _product.Image ?? string.Empty;
            ImageAltTextBox.Text    = _product.ImageAlt ?? string.Empty;

            if (_product.IdCategorie.HasValue)
                CategoryComboBox.SelectedValue = _product.IdCategorie.Value;

            SelectTypeCard(_product.TypeVente);

            var ci = System.Globalization.CultureInfo.InvariantCulture;
            switch (_product.TypeVente)
            {
                case 0:
                    PrixTtcBuyBox.Text  = _product.Prix?.ToString("F2", ci) ?? string.Empty;
                    PrixHtBuyBox.Text   = _product.PrixHt?.ToString("F2", ci) ?? string.Empty;
                    QuantityBuyBox.Text = _product.Quantity.ToString();
                    break;
                case 2:
                    PrixAuctionBox.Text    = _product.Prix?.ToString("F2", ci) ?? string.Empty;
                    QuantityAuctionBox.Text = _product.Quantity.ToString();
                    if (_product.GroupExpiresAt.HasValue)
                    {
                        AuctionEndDatePicker.SelectedDate = _product.GroupExpiresAt.Value.Date;
                        AuctionEndTimeBox.Text = _product.GroupExpiresAt.Value.ToString("HH:mm");
                    }
                    break;
                case 1:
                    PrixTtcGroupBox.Text  = _product.Prix?.ToString("F2", ci) ?? string.Empty;
                    PrixHtGroupBox.Text   = _product.PrixHt?.ToString("F2", ci) ?? string.Empty;
                    QuantityGroupBox.Text = _product.Quantity.ToString();
                    GroupBuyersBox.Text   = _product.GroupRequiredBuyers?.ToString() ?? "1";
                    if (_product.GroupExpiresAt.HasValue)
                    {
                        GroupDeadlineDatePicker.SelectedDate = _product.GroupExpiresAt.Value.Date;
                        GroupDeadlineTimeBox.Text = _product.GroupExpiresAt.Value.ToString("HH:mm");
                    }
                    break;
            }

            LoadImagePreview(_product.ImageAbsolutePath);
        }

        // ═══ IMAGE ══════════════════════════════════════════════════════════

        private void ImageDrop_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files.Length == 1 && IsValidImageFile(files[0]))
                { e.Effects = DragDropEffects.Copy; e.Handled = true; return; }
            }
            e.Effects = DragDropEffects.None; e.Handled = true;
        }

        private void ImageDrop_Drop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files.Length != 1) return;
            if (!IsValidImageFile(files[0])) { ShowStatus("Format non supporté.", isError: true); return; }
            if (!IsValidImageSize(files[0]))  { ShowStatus("Image trop volumineuse (max 5 Mo).", isError: true); return; }
            SetImageFile(files[0]);
        }

        private void ImagePreview_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Filter = "Images (*.jpg;*.jpeg;*.png;*.gif;*.bmp;*.webp)|*.jpg;*.jpeg;*.png;*.gif;*.bmp;*.webp|Tous les fichiers (*.*)|*.*",
                Title  = "Sélectionner une image produit"
            };
            if (dlg.ShowDialog() != true) return;
            if (!IsValidImageSize(dlg.FileName)) { ShowStatus("Image trop volumineuse (max 5 Mo).", isError: true); return; }
            SetImageFile(dlg.FileName);
        }

        private void SetImageFile(string filePath)
        {
            _selectedImagePath = filePath;
            ImageUrlTextBox.Text = filePath;
            LoadImagePreview(filePath);
            ShowStatus("Image sélectionnée", isError: false);
        }

        private void LoadImagePreview(string? imagePath)
        {
            if (!string.IsNullOrWhiteSpace(imagePath) && File.Exists(imagePath))
            {
                try
                {
                    var bmp = new BitmapImage();
                    bmp.BeginInit();
                    bmp.UriSource = new Uri(imagePath, UriKind.Absolute);
                    bmp.CacheOption = BitmapCacheOption.OnLoad;
                    bmp.DecodePixelHeight = 260;
                    bmp.EndInit(); bmp.Freeze();
                    ImagePreview.Source = bmp;
                    ImageDropPlaceholder.Visibility = Visibility.Collapsed;
                    return;
                }
                catch { /* fall through */ }
            }
            ImagePreview.Source = null;
            ImageDropPlaceholder.Visibility = Visibility.Visible;
        }

        private static bool IsValidImageFile(string path) =>
            AllowedImageExtensions.Contains(Path.GetExtension(path).ToLowerInvariant());

        private static bool IsValidImageSize(string path)
        {
            try { return new FileInfo(path).Length <= MaxImageSizeBytes; }
            catch { return false; }
        }

        private static string? CopyImageToAppFolder(string sourcePath)
        {
            try
            {
                var dir  = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images");
                Directory.CreateDirectory(dir);
                var dest = Path.Combine(dir, $"{Guid.NewGuid():N}{Path.GetExtension(sourcePath)}");
                File.Copy(sourcePath, dest, overwrite: true);
                return dest;
            }
            catch { return sourcePath; }
        }

        // ═══ SAVE ════════════════════════════════════════════════════════════

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInputs()) return;

            var button = sender as Button;
            if (button != null) button.IsEnabled = false;
            ShowStatus("Enregistrement en cours...", isError: false);

            try
            {
                using var context = new DatabaseContext();

                var imagePath = _selectedImagePath != null
                    ? CopyImageToAppFolder(_selectedImagePath)
                    : (string.IsNullOrWhiteSpace(ImageUrlTextBox.Text) ? null : ImageUrlTextBox.Text.Trim());

                CollectSectionValues(out var prix, out var prixHt, out var tauxTva,
                    out var quantity, out var groupBuyers, out var groupExpiresAt);

                if (_isEditMode && _product != null)
                {
                    var p = await context.Produits.FirstOrDefaultAsync(x => x.IdProduit == _product.IdProduit);
                    if (p == null) { ShowStatus("Produit introuvable", isError: true); return; }

                    if (AuthenticationService.CurrentSeller != null &&
                        p.IdVendeur != AuthenticationService.CurrentSeller.IdUser)
                    { ShowStatus("Accès refusé : ce produit ne vous appartient pas", isError: true); return; }

                    ApplyValues(p, imagePath, prix, prixHt, tauxTva, quantity, groupBuyers, groupExpiresAt);
                    p.UpdatedAt = DateTime.Now;
                }
                else
                {
                    if (!AuthenticationService.IsAuthenticated || AuthenticationService.CurrentSeller == null)
                    { ShowStatus("Session expirée. Reconnectez-vous.", isError: true); return; }

                    var newProduct = new Produit { IdVendeur = AuthenticationService.CurrentSeller.IdUser, CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now };
                    ApplyValues(newProduct, imagePath, prix, prixHt, tauxTva, quantity, groupBuyers, groupExpiresAt);
                    context.Produits.Add(newProduct);
                }

                await context.SaveChangesAsync();
                ShowStatus("Produit enregistré avec succès !", isError: false);
                await System.Threading.Tasks.Task.Delay(700);
                DialogResult = true;
                Close();
            }
            catch (Exception ex) { ShowStatus($"Erreur d'enregistrement: {ex.Message}", isError: true); }
            finally { if (button != null) button.IsEnabled = true; }
        }

        private void ApplyValues(Produit p, string? imagePath,
            decimal prix, decimal prixHt, decimal tauxTva,
            int quantity, int? groupBuyers, DateTime? groupExpiresAt)
        {
            p.Description        = SanitizeInput(DescriptionTextBox.Text);
            p.Prix               = prix;
            p.PrixHt             = prixHt;
            p.TauxTva            = tauxTva;
            p.Quantity           = quantity;
            p.TypeVente          = _selectedTypeVente;
            p.IdCategorie        = (int?)CategoryComboBox.SelectedValue;
            p.GroupRequiredBuyers = groupBuyers;
            p.GroupExpiresAt     = groupExpiresAt;
            p.Image              = imagePath;
            p.ImageAlt           = string.IsNullOrWhiteSpace(ImageAltTextBox.Text)
                                   ? null : SanitizeInput(ImageAltTextBox.Text);
        }

        private void CollectSectionValues(out decimal prix, out decimal prixHt, out decimal tauxTva,
            out int quantity, out int? groupBuyers, out DateTime? groupExpiresAt)
        {
            groupBuyers    = null;
            groupExpiresAt = null;
            tauxTva        = 20m;

            switch (_selectedTypeVente)
            {
                case 0:
                    _ = TryParseDecimal(PrixTtcBuyBox.Text, out prix);
                    _ = TryParseDecimal(PrixHtBuyBox.Text, out prixHt);
                    _ = int.TryParse(QuantityBuyBox.Text.Trim(), out quantity);
                    break;
                case 2:
                    _ = TryParseDecimal(PrixAuctionBox.Text, out prix);
                    prixHt = prix; tauxTva = 0;
                    _ = int.TryParse(QuantityAuctionBox.Text.Trim(), out quantity);
                    groupExpiresAt = GetDateTime(AuctionEndDatePicker, AuctionEndTimeBox);
                    break;
                case 1:
                    _ = TryParseDecimal(PrixTtcGroupBox.Text, out prix);
                    _ = TryParseDecimal(PrixHtGroupBox.Text, out prixHt);
                    _ = int.TryParse(QuantityGroupBox.Text.Trim(), out quantity);
                    _ = int.TryParse(GroupBuyersBox.Text.Trim(), out var buyers);
                    groupBuyers    = buyers > 0 ? buyers : 1;
                    groupExpiresAt = GetDateTime(GroupDeadlineDatePicker, GroupDeadlineTimeBox);
                    break;
                default: prix = prixHt = 0; quantity = 1; break;
            }
        }

        // ═══ DELETE ═════════════════════════════════════════════════════════

        private async void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (_product == null) return;
            if (!NeuDialog.Confirm(this, "Confirmer la suppression",
                $"Voulez-vous vraiment supprimer ?\n\n{_product.Description}\nPrix : {_product.PrixFormate}\n\nCette action est irréversible !"))
                return;

            var button = sender as Button;
            if (button != null) button.IsEnabled = false;
            ShowStatus("Suppression en cours...", isError: false);

            try
            {
                using var context = new DatabaseContext();
                var p = await context.Produits.FirstOrDefaultAsync(x => x.IdProduit == _product.IdProduit);
                if (p == null) { ShowStatus("Produit introuvable", isError: true); return; }

                if (AuthenticationService.CurrentSeller != null &&
                    p.IdVendeur != AuthenticationService.CurrentSeller.IdUser)
                { ShowStatus("Accès refusé", isError: true); return; }

                context.Produits.Remove(p);
                await context.SaveChangesAsync();
                ShowStatus("Produit supprimé !", isError: false);
                await System.Threading.Tasks.Task.Delay(700);
                DialogResult = true; Close();
            }
            catch (Exception ex) { ShowStatus($"Erreur: {ex.Message}", isError: true); }
            finally { if (button != null) button.IsEnabled = true; }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e) { DialogResult = false; Close(); }

        // ═══ VALIDATION ═════════════════════════════════════════════════════

        private bool ValidateInputs()
        {
            if (string.IsNullOrWhiteSpace(DescriptionTextBox.Text))
            { ShowStatus("La description est requise", isError: true); DescriptionTextBox.Focus(); return false; }

            switch (_selectedTypeVente)
            {
                case 0:
                    if (!TryParseDecimal(PrixTtcBuyBox.Text, out var pb) || pb <= 0)
                    { ShowStatus("Prix TTC valide requis (> 0)", isError: true); PrixTtcBuyBox.Focus(); return false; }
                    if (!int.TryParse(QuantityBuyBox.Text.Trim(), out var qb) || qb < 0)
                    { ShowStatus("Quantité valide requise (≥ 0)", isError: true); QuantityBuyBox.Focus(); return false; }
                    break;
                case 2:
                    if (!TryParseDecimal(PrixAuctionBox.Text, out var pa) || pa <= 0)
                    { ShowStatus("Prix de départ valide requis (> 0)", isError: true); PrixAuctionBox.Focus(); return false; }
                    if (!AuctionEndDatePicker.SelectedDate.HasValue || AuctionEndDatePicker.SelectedDate.Value.Date <= DateTime.Today)
                    { ShowStatus("Date de fin d'enchère future requise", isError: true); return false; }
                    if (!int.TryParse(QuantityAuctionBox.Text.Trim(), out var qa) || qa < 0)
                    { ShowStatus("Quantité valide requise (≥ 0)", isError: true); QuantityAuctionBox.Focus(); return false; }
                    break;
                case 1:
                    if (!TryParseDecimal(PrixTtcGroupBox.Text, out var pg) || pg <= 0)
                    { ShowStatus("Prix TTC valide requis (> 0)", isError: true); PrixTtcGroupBox.Focus(); return false; }
                    if (!int.TryParse(GroupBuyersBox.Text.Trim(), out var bg) || bg < 1)
                    { ShowStatus("Nombre d'acheteurs requis ≥ 1", isError: true); GroupBuyersBox.Focus(); return false; }
                    if (!GroupDeadlineDatePicker.SelectedDate.HasValue || GroupDeadlineDatePicker.SelectedDate.Value.Date <= DateTime.Today)
                    { ShowStatus("Date/heure limite future requise", isError: true); return false; }
                    if (!int.TryParse(QuantityGroupBox.Text.Trim(), out var qg) || qg < 0)
                    { ShowStatus("Quantité valide requise (≥ 0)", isError: true); QuantityGroupBox.Focus(); return false; }
                    break;
            }
            return true;
        }

        // ═══ HELPERS ════════════════════════════════════════════════════════

        private static bool TryParseDecimal(string text, out decimal value) =>
            decimal.TryParse(text.Trim().Replace(',', '.'),
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out value);

        private static DateTime? GetDateTime(DatePicker dp, TextBox tb)
        {
            if (!dp.SelectedDate.HasValue) return null;
            if (!TimeSpan.TryParseExact(tb.Text.Trim(), @"hh\:mm", null, out var t)) t = TimeSpan.Zero;
            return dp.SelectedDate.Value.Date + t;
        }

        private static string SanitizeInput(string input) =>
            string.IsNullOrWhiteSpace(input) ? string.Empty
            : input.Trim().Replace("<", "&lt;").Replace(">", "&gt;");

        private void ShowStatus(string message, bool isError)
        {
            StatusTextBlock.Text = message;
            StatusTextBlock.Foreground = isError
                ? (System.Windows.Media.Brush)FindResource("NeuDangerBrush")
                : (System.Windows.Media.Brush)FindResource("NeuSuccessBrush");
            StatusBorder.Visibility = Visibility.Visible;
            FooterTextBlock.Text = isError ? "Erreur" : "Succès";
        }
    }
}
