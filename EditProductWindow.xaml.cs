using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using GroupeV.Controls;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;

namespace GroupeV
{
    /// <summary>
    /// Edit Product Window - Create or edit products with image support.
    /// </summary>
    public partial class EditProductWindow : Window
    {
        private static readonly string[] AllowedImageExtensions = [".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp"];
        private const long MaxImageSizeBytes = 5 * 1024 * 1024; // 5 MB

        private Produit? _product;
        private bool _isEditMode;
        private string? _selectedImagePath;

        /// <summary>
        /// Create new product
        /// </summary>
        public EditProductWindow()
        {
            InitializeComponent();
            _isEditMode = false;
            HeaderTextBlock.Text = "Nouveau Produit";
            Loaded += EditProductWindow_Loaded;
        }

        /// <summary>
        /// Edit existing product
        /// </summary>
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
            // Vérification de propriété en mode édition
            if (_isEditMode && _product != null && AuthenticationService.CurrentSeller != null)
            {
                if (_product.IdVendeur != AuthenticationService.CurrentSeller.IdUser)
                {
                    NeuDialog.ShowWarning(this, "Accès refusé",
                        "Vous ne pouvez modifier que vos propres produits.");
                    Close();
                    return;
                }
            }

            await LoadCategoriesAsync();

            if (_isEditMode && _product != null)
            {
                LoadProductData();
            }
        }

        private async System.Threading.Tasks.Task LoadCategoriesAsync()
        {
            try
            {
                using var context = new DatabaseContext();
                var categories = await context.Categories
                    .OrderBy(c => c.Libelle)
                    .ToListAsync();

                CategoryComboBox.ItemsSource = categories;

                if (_product != null && _product.IdCategorie.HasValue)
                {
                    CategoryComboBox.SelectedValue = _product.IdCategorie.Value;
                }
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
            DescriptionTextBox.Text = _product.Description ?? "";
            PriceTextBox.Text = _product.Prix?.ToString("F2") ?? "";
            ImageUrlTextBox.Text = _product.Image ?? "";
            ImageAltTextBox.Text = _product.ImageAlt ?? "";

            if (_product.IdCategorie.HasValue)
            {
                CategoryComboBox.SelectedValue = _product.IdCategorie.Value;
            }

            // Sélectionner le type de vente
            TypeVenteComboBox.SelectedIndex = _product.TypeVente;

            // Charger l'aperçu de l'image
            LoadImagePreview(_product.ImageAbsolutePath);
        }

        // ========== IMAGE DRAG & DROP / BROWSE ==========

        private void ImageDrop_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files.Length == 1 && IsValidImageFile(files[0]))
                {
                    e.Effects = DragDropEffects.Copy;
                    e.Handled = true;
                    return;
                }
            }
            e.Effects = DragDropEffects.None;
            e.Handled = true;
        }

        private void ImageDrop_Drop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;

            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files.Length != 1) return;

            var filePath = files[0];
            if (!IsValidImageFile(filePath))
            {
                ShowStatus("Format d'image non supporté. Utilisez JPG, PNG, GIF ou BMP.", isError: true);
                return;
            }

            if (!IsValidImageSize(filePath))
            {
                ShowStatus("L'image est trop volumineuse (max 5 Mo).", isError: true);
                return;
            }

            SetImageFile(filePath);
        }

        private void ImagePreview_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var openDialog = new OpenFileDialog
            {
                Filter = "Images (*.jpg;*.jpeg;*.png;*.gif;*.bmp;*.webp)|*.jpg;*.jpeg;*.png;*.gif;*.bmp;*.webp|Tous les fichiers (*.*)|*.*",
                Title = "Sélectionner une image produit"
            };

            if (openDialog.ShowDialog() != true) return;

            if (!IsValidImageSize(openDialog.FileName))
            {
                ShowStatus("L'image est trop volumineuse (max 5 Mo).", isError: true);
                return;
            }

            SetImageFile(openDialog.FileName);
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
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(imagePath, UriKind.Absolute);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.DecodePixelHeight = 280;
                    bitmap.EndInit();
                    bitmap.Freeze();

                    ImagePreview.Source = bitmap;
                    ImageDropPlaceholder.Visibility = Visibility.Collapsed;
                }
                catch
                {
                    ImagePreview.Source = null;
                    ImageDropPlaceholder.Visibility = Visibility.Visible;
                }
            }
            else
            {
                ImagePreview.Source = null;
                ImageDropPlaceholder.Visibility = Visibility.Visible;
            }
        }

        private static bool IsValidImageFile(string path)
        {
            var ext = Path.GetExtension(path).ToLowerInvariant();
            return AllowedImageExtensions.Contains(ext);
        }

        private static bool IsValidImageSize(string path)
        {
            try
            {
                var info = new FileInfo(path);
                return info.Length <= MaxImageSizeBytes;
            }
            catch
            {
                return false;
            }
        }

        // ========== COPY IMAGE TO APP FOLDER ==========

        private string? CopyImageToAppFolder(string sourcePath)
        {
            try
            {
                var imagesDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images");
                Directory.CreateDirectory(imagesDir);

                var safeFileName = $"{Guid.NewGuid():N}{Path.GetExtension(sourcePath)}";
                var destPath = Path.Combine(imagesDir, safeFileName);
                File.Copy(sourcePath, destPath, overwrite: true);
                return destPath;
            }
            catch
            {
                return sourcePath;
            }
        }

        // ========== SAVE / DELETE ==========

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

                if (_isEditMode && _product != null)
                {
                    var productToUpdate = await context.Produits
                        .FirstOrDefaultAsync(p => p.IdProduit == _product.IdProduit);

                    if (productToUpdate == null)
                    {
                        ShowStatus("Produit introuvable", isError: true);
                        return;
                    }

                    // Double vérification de la propriété côté serveur
                    if (AuthenticationService.CurrentSeller != null &&
                        productToUpdate.IdVendeur != AuthenticationService.CurrentSeller.IdUser)
                    {
                        ShowStatus("Accès refusé : ce produit ne vous appartient pas", isError: true);
                        return;
                    }

                    productToUpdate.Description = SanitizeInput(DescriptionTextBox.Text);
                    productToUpdate.Prix = decimal.Parse(PriceTextBox.Text.Trim());
                    productToUpdate.IdCategorie = (int?)CategoryComboBox.SelectedValue;
                    productToUpdate.TypeVente = TypeVenteComboBox.SelectedIndex;
                    productToUpdate.Image = imagePath;
                    productToUpdate.ImageAlt = string.IsNullOrWhiteSpace(ImageAltTextBox.Text)
                        ? null : SanitizeInput(ImageAltTextBox.Text);
                    productToUpdate.UpdatedAt = DateTime.Now;
                }
                else
                {
                    if (!AuthenticationService.IsAuthenticated || AuthenticationService.CurrentSeller == null)
                    {
                        ShowStatus("Session expirée. Reconnectez-vous.", isError: true);
                        return;
                    }

                    var newProduct = new Produit
                    {
                        Description = SanitizeInput(DescriptionTextBox.Text),
                        Prix = decimal.Parse(PriceTextBox.Text.Trim()),
                        IdCategorie = (int?)CategoryComboBox.SelectedValue,
                        TypeVente = TypeVenteComboBox.SelectedIndex,
                        IdVendeur = AuthenticationService.CurrentSeller.IdUser,
                        Image = imagePath,
                        ImageAlt = string.IsNullOrWhiteSpace(ImageAltTextBox.Text)
                            ? null : SanitizeInput(ImageAltTextBox.Text),
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now
                    };

                    context.Produits.Add(newProduct);
                }

                await context.SaveChangesAsync();

                ShowStatus("Produit enregistré avec succès !", isError: false);
                await System.Threading.Tasks.Task.Delay(800);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                ShowStatus($"Erreur d'enregistrement: {ex.Message}", isError: true);
            }
            finally
            {
                if (button != null) button.IsEnabled = true;
            }
        }

        private async void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (_product == null) return;

            if (!NeuDialog.Confirm(this, "Confirmer la suppression",
                $"Voulez-vous vraiment supprimer ce produit ?\n\n" +
                $"Description : {_product.Description}\n" +
                $"Prix : {_product.PrixFormate}\n\n" +
                "Cette action est irréversible !"))
            {
                return;
            }

            var button = sender as Button;
            if (button != null) button.IsEnabled = false;
            ShowStatus("Suppression en cours...", isError: false);

            try
            {
                using var context = new DatabaseContext();

                var productToDelete = await context.Produits
                    .FirstOrDefaultAsync(p => p.IdProduit == _product.IdProduit);

                if (productToDelete == null)
                {
                    ShowStatus("Produit introuvable", isError: true);
                    return;
                }

                // Double vérification de la propriété
                if (AuthenticationService.CurrentSeller != null &&
                    productToDelete.IdVendeur != AuthenticationService.CurrentSeller.IdUser)
                {
                    ShowStatus("Accès refusé : ce produit ne vous appartient pas", isError: true);
                    return;
                }

                context.Produits.Remove(productToDelete);
                await context.SaveChangesAsync();

                ShowStatus("Produit supprimé avec succès !", isError: false);
                await System.Threading.Tasks.Task.Delay(800);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                ShowStatus($"Erreur de suppression: {ex.Message}", isError: true);
            }
            finally
            {
                if (button != null) button.IsEnabled = true;
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private bool ValidateInputs()
        {
            if (string.IsNullOrWhiteSpace(DescriptionTextBox.Text))
            {
                ShowStatus("La description du produit est requise", isError: true);
                DescriptionTextBox.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(PriceTextBox.Text) ||
                !decimal.TryParse(PriceTextBox.Text.Trim(), out var price) || price <= 0 || price > 999999.99m)
            {
                ShowStatus("Un prix valide est requis (entre 0 et 999 999.99)", isError: true);
                PriceTextBox.Focus();
                return false;
            }

            if (CategoryComboBox.SelectedValue == null)
            {
                ShowStatus("Veuillez sélectionner une catégorie", isError: true);
                CategoryComboBox.Focus();
                return false;
            }

            return true;
        }

        /// <summary>
        /// Nettoie les entrées utilisateur pour éviter les injections XSS dans les champs texte.
        /// </summary>
        private static string SanitizeInput(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;
            return input.Trim()
                .Replace("<", "&lt;")
                .Replace(">", "&gt;");
        }

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
