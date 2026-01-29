using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;

namespace GroupeV
{
    /// <summary>
    /// Edit Product Window - Create or edit products
    /// </summary>
    public partial class EditProductWindow : Window
    {
        private Produit? _product;
        private bool _isEditMode;

        /// <summary>
        /// Create new product
        /// </summary>
        public EditProductWindow()
        {
            InitializeComponent();
            _isEditMode = false;
            HeaderTextBlock.Text = "[CREATE PRODUCT]";
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
            HeaderTextBlock.Text = "[EDIT PRODUCT]";
            DeleteButton.Visibility = Visibility.Visible;
            ProductIdBorder.Visibility = Visibility.Visible;
            Loaded += EditProductWindow_Loaded;
        }

        private async void EditProductWindow_Loaded(object sender, RoutedEventArgs e)
        {
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
                ShowStatus($"? Error loading categories: {ex.Message}", isError: true);
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
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInputs())
            {
                return;
            }

            var button = sender as Button;
            if (button != null) button.IsEnabled = false;
            ShowStatus("? Saving product...", isError: false);

            try
            {
                using var context = new DatabaseContext();

                if (_isEditMode && _product != null)
                {
                    // Update existing product
                    var productToUpdate = await context.Produits
                        .FirstOrDefaultAsync(p => p.IdProduit == _product.IdProduit);

                    if (productToUpdate == null)
                    {
                        ShowStatus("? Product not found", isError: true);
                        return;
                    }

                    productToUpdate.Description = DescriptionTextBox.Text.Trim();
                    productToUpdate.Prix = decimal.Parse(PriceTextBox.Text.Trim());
                    productToUpdate.IdCategorie = (int?)CategoryComboBox.SelectedValue;
                    productToUpdate.Image = string.IsNullOrWhiteSpace(ImageUrlTextBox.Text) ? null : ImageUrlTextBox.Text.Trim();
                    productToUpdate.ImageAlt = string.IsNullOrWhiteSpace(ImageAltTextBox.Text) ? null : ImageAltTextBox.Text.Trim();
                    productToUpdate.UpdatedAt = DateTime.Now;
                }
                else
                {
                    // Create new product
                    if (!AuthenticationService.IsAuthenticated || AuthenticationService.CurrentSeller == null)
                    {
                        ShowStatus("? No authenticated seller", isError: true);
                        return;
                    }

                    var newProduct = new Produit
                    {
                        Description = DescriptionTextBox.Text.Trim(),
                        Prix = decimal.Parse(PriceTextBox.Text.Trim()),
                        IdCategorie = (int?)CategoryComboBox.SelectedValue,
                        IdVendeur = AuthenticationService.CurrentSeller.IdUser,
                        Image = string.IsNullOrWhiteSpace(ImageUrlTextBox.Text) ? null : ImageUrlTextBox.Text.Trim(),
                        ImageAlt = string.IsNullOrWhiteSpace(ImageAltTextBox.Text) ? null : ImageAltTextBox.Text.Trim(),
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now
                    };

                    context.Produits.Add(newProduct);
                }

                await context.SaveChangesAsync();

                ShowStatus("? Product saved successfully!", isError: false);
                await System.Threading.Tasks.Task.Delay(1000);

                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                ShowStatus($"? Save error: {ex.Message}", isError: true);
            }
            finally
            {
                if (button != null) button.IsEnabled = true;
            }
        }

        private async void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (_product == null) return;

            var result = MessageBox.Show(this,
                $"Are you sure you want to delete this product?\n\n" +
                $"Description: {_product.Description}\n" +
                $"Price: {_product.PrixFormate}\n\n" +
                "This action cannot be undone!",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            var button = sender as Button;
            if (button != null) button.IsEnabled = false;
            ShowStatus("? Deleting product...", isError: false);

            try
            {
                using var context = new DatabaseContext();

                var productToDelete = await context.Produits
                    .FirstOrDefaultAsync(p => p.IdProduit == _product.IdProduit);

                if (productToDelete == null)
                {
                    ShowStatus("? Product not found", isError: true);
                    return;
                }

                context.Produits.Remove(productToDelete);
                await context.SaveChangesAsync();

                ShowStatus("? Product deleted successfully!", isError: false);
                await System.Threading.Tasks.Task.Delay(1000);

                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                ShowStatus($"? Delete error: {ex.Message}", isError: true);
            }
            finally
            {
                if (button != null) button.IsEnabled = true;
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private bool ValidateInputs()
        {
            if (string.IsNullOrWhiteSpace(DescriptionTextBox.Text))
            {
                ShowStatus("? Product description is required", isError: true);
                DescriptionTextBox.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(PriceTextBox.Text) || !decimal.TryParse(PriceTextBox.Text.Trim(), out var price) || price <= 0)
            {
                ShowStatus("? Valid price is required (must be greater than 0)", isError: true);
                PriceTextBox.Focus();
                return false;
            }

            if (CategoryComboBox.SelectedValue == null)
            {
                ShowStatus("? Please select a category", isError: true);
                CategoryComboBox.Focus();
                return false;
            }

            return true;
        }

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
            FooterTextBlock.Text = $"[STATUS] {(isError ? "Error" : "Success")}";
        }
    }
}
