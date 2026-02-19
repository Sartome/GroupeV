using System;
using System.Windows;
using System.Windows.Media;
using GroupeV.Controls;
using Microsoft.EntityFrameworkCore;

namespace GroupeV
{
    /// <summary>
    /// Allows the current seller to edit their personal profile information.
    /// </summary>
    public partial class EditProfileWindow : Window
    {
        public EditProfileWindow()
        {
            InitializeComponent();
            LoadCurrentProfile();
        }

        private void LoadCurrentProfile()
        {
            var user = AuthenticationService.CurrentUser;
            var seller = AuthenticationService.CurrentSeller;
            if (user == null) return;

            PrenomBox.Text = user.Prenom ?? string.Empty;
            NomBox.Text = user.Nom ?? string.Empty;
            EmailBox.Text = user.Email ?? string.Empty;
            PhoneBox.Text = user.Phone ?? string.Empty;
            AdresseBox.Text = user.Adresse ?? string.Empty;
            NomEntrepriseBox.Text = seller?.NomEntreprise ?? string.Empty;
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            var prenom = PrenomBox.Text.Trim();
            var nom = NomBox.Text.Trim();
            var email = EmailBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(prenom) || string.IsNullOrWhiteSpace(nom))
            {
                ShowStatus("Le prénom et le nom sont obligatoires.", isError: true);
                return;
            }

            if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            {
                ShowStatus("Adresse email invalide.", isError: true);
                return;
            }

            SaveButton.IsEnabled = false;
            ShowStatus("Enregistrement en cours...", isError: false);

            try
            {
                using var ctx = new DatabaseContext();

                var user = await ctx.Utilisateurs.FirstOrDefaultAsync(
                    u => u.IdUser == AuthenticationService.CurrentUser!.IdUser);

                if (user == null)
                {
                    ShowStatus("Utilisateur introuvable.", isError: true);
                    return;
                }

                user.Prenom = prenom;
                user.Nom = nom;
                user.Email = email;
                user.Phone = string.IsNullOrWhiteSpace(PhoneBox.Text) ? null : PhoneBox.Text.Trim();
                user.Adresse = string.IsNullOrWhiteSpace(AdresseBox.Text) ? null : AdresseBox.Text.Trim();

                var seller = await ctx.Vendeurs.FirstOrDefaultAsync(
                    v => v.IdUser == AuthenticationService.CurrentSeller!.IdUser);

                if (seller != null)
                    seller.NomEntreprise = string.IsNullOrWhiteSpace(NomEntrepriseBox.Text)
                        ? null : NomEntrepriseBox.Text.Trim();

                await ctx.SaveChangesAsync();

                // Update session cache
                AuthenticationService.CurrentUser!.Prenom = user.Prenom;
                AuthenticationService.CurrentUser!.Nom = user.Nom;
                AuthenticationService.CurrentUser!.Email = user.Email;
                AuthenticationService.CurrentUser!.Phone = user.Phone;
                AuthenticationService.CurrentUser!.Adresse = user.Adresse;
                if (AuthenticationService.CurrentSeller != null && seller != null)
                    AuthenticationService.CurrentSeller.NomEntreprise = seller.NomEntreprise;

                DialogResult = true;
                NeuDialog.ShowSuccess(this, "Profil mis à jour", "Vos informations ont été enregistrées.");
                Close();
            }
            catch (Exception ex)
            {
                ShowStatus($"Erreur : {ex.Message}", isError: true);
            }
            finally
            {
                SaveButton.IsEnabled = true;
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e) => Close();

        private void ShowStatus(string message, bool isError)
        {
            StatusText.Text = message;
            StatusText.Foreground = isError
                ? (SolidColorBrush)FindResource("NeuDangerBrush")
                : (SolidColorBrush)FindResource("NeuSuccessBrush");
            StatusBorder.Visibility = Visibility.Visible;
        }
    }
}
