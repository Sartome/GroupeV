using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace GroupeV
{
    /// <summary>
    /// Aide au diagnostic pour résoudre les problèmes de connexion
    /// </summary>
    public static class LoginDiagnostics
    {
        /// <summary>
        /// Tester la connexion et fournir des informations de diagnostic détaillées
        /// </summary>
        public static async Task<LoginDiagnosticResult> DiagnoseLoginAsync(string email, string password)
        {
            var result = new LoginDiagnosticResult { Email = email };

            try
            {
                // Étape 1: Vérifier la connexion à la base de données
                var connectionCheck = await DatabaseHelper.CheckConnectionAsync();
                result.ConnectionSuccess = connectionCheck.success;
                result.ConnectionMessage = connectionCheck.message;

                if (!connectionCheck.success)
                {
                    result.FailureReason = "Échec de la connexion à la base de données";
                    return result;
                }

                using var context = new DatabaseContext();

                // Étape 2: Compter le nombre total d'utilisateurs
                result.TotalUsersInDb = await context.Utilisateurs.CountAsync();

                // Étape 3: Trouver l'utilisateur par email (insensible à la casse)
                var emailLower = email.ToLowerInvariant();
                var user = await context.Utilisateurs
                    .FirstOrDefaultAsync(u => u.Email != null && u.Email.ToLower() == emailLower);

                result.UserFound = user != null;

                if (user == null)
                {
                    result.FailureReason = "Utilisateur non trouvé avec cet email";
                    // Lister tous les emails pour le débogage (premiers 10)
                    result.AvailableEmails = await context.Utilisateurs
                        .Take(10)
                        .Select(u => u.Email ?? "(null)")
                        .ToListAsync();
                    return result;
                }

                result.UserId = user.IdUser;
                result.StoredEmail = user.Email ?? "(null)";
                result.StoredPassword = user.MotDePasse ?? "(null)";
                result.PasswordLength = user.MotDePasse?.Length ?? 0;
                result.PasswordHashType = PasswordVerifier.GetHashTypeName(user.MotDePasse);

                // Étape 4: Vérifier si l'utilisateur est un vendeur
                result.TotalSellersInDb = await context.Vendeurs.CountAsync();
                var seller = await context.Vendeurs
                    .Include(v => v.Utilisateur)
                    .FirstOrDefaultAsync(v => v.IdUser == user.IdUser);

                result.IsSeller = seller != null;

                if (seller == null)
                {
                    result.FailureReason = "L'utilisateur existe mais n'est pas un vendeur";
                    return result;
                }

                result.SellerCompany = seller.NomEntreprise;
                result.IsCertified = seller.IsCertified;

                // Étape 5: Vérifier le mot de passe (en utilisant PasswordVerifier pour le support des hachages)
                result.PasswordMatches = PasswordVerifier.VerifyPassword(password, user.MotDePasse);

                if (!result.PasswordMatches)
                {
                    result.FailureReason = "Le mot de passe ne correspond pas";
                    result.PasswordComparison = $"Type de hachage: {result.PasswordHashType}";
                    return result;
                }

                // Tous les vendeurs ont accès - certification non requise
                result.LoginSuccess = true;
                result.FailureReason = null;
            }
            catch (Exception ex)
            {
                result.FailureReason = $"Exception: {ex.Message}";
                result.ExceptionDetails = ex.ToString();
            }

            return result;
        }

        /// <summary>
        /// Lister tous les vendeurs dans la base de données avec leurs emails
        /// </summary>
        public static async Task<string> ListAllSellersAsync()
        {
            try
            {
                using var context = new DatabaseContext();
                
                var sellers = await context.Vendeurs
                    .Include(v => v.Utilisateur)
                    .Select(v => new
                    {
                        UserId = v.IdUser,
                        Email = v.Utilisateur!.Email ?? "(pas d'email)",
                        Name = v.Utilisateur.NomComplet,
                        Company = v.NomEntreprise ?? "(pas d'entreprise)",
                        Certified = v.IsCertified
                    })
                    .ToListAsync();

                if (sellers.Count == 0)
                {
                    return "Aucun vendeur trouvé dans la base de données.";
                }

                var output = $"Trouvé {sellers.Count} vendeur(s):\n\n";
                foreach (var s in sellers)
                {
                    var status = s.Certified ? "? Certifié" : "? Non certifié";
                    output += $"• ID: {s.UserId}\n";
                    output += $"  Email: {s.Email}\n";
                    output += $"  Nom: {s.Name}\n";
                    output += $"  Entreprise: {s.Company}\n";
                    output += $"  Statut: {status}\n\n";
                }

                return output;
            }
            catch (Exception ex)
            {
                return $"Erreur lors de la liste des vendeurs: {ex.Message}";
            }
        }
    }

    /// <summary>
    /// Résultat de la vérification de diagnostic de connexion
    /// </summary>
    public class LoginDiagnosticResult
    {
        public string Email { get; set; } = string.Empty;
        public bool ConnectionSuccess { get; set; }
        public string ConnectionMessage { get; set; } = string.Empty;
        
        public int TotalUsersInDb { get; set; }
        public int TotalSellersInDb { get; set; }
        
        public bool UserFound { get; set; }
        public int UserId { get; set; }
        public string StoredEmail { get; set; } = string.Empty;
        public string StoredPassword { get; set; } = string.Empty;
        public int PasswordLength { get; set; }
        public string PasswordHashType { get; set; } = string.Empty;
        
        public bool IsSeller { get; set; }
        public string? SellerCompany { get; set; }
        public bool IsCertified { get; set; }
        
        public bool PasswordMatches { get; set; }
        public string? PasswordComparison { get; set; }
        
        public bool LoginSuccess { get; set; }
        public string? FailureReason { get; set; }
        public string? ExceptionDetails { get; set; }
        public System.Collections.Generic.List<string>? AvailableEmails { get; set; }

        public override string ToString()
        {
            var output = "=== RAPPORT DE DIAGNOSTIC DE CONNEXION ===\n\n";
            output += $"Email: {Email}\n";
            output += $"Connexion: {(ConnectionSuccess ? "? Succès" : "? Échec")} - {ConnectionMessage}\n\n";
            
            if (!ConnectionSuccess)
                return output + $"RÉSULTAT: {FailureReason}";

            output += $"Statistiques de la base de données:\n";
            output += $"  Total des utilisateurs: {TotalUsersInDb}\n";
            output += $"  Total des vendeurs: {TotalSellersInDb}\n\n";

            output += $"Recherche d'utilisateur: {(UserFound ? "? Trouvé" : "? Non trouvé")}\n";
            if (UserFound)
            {
                output += $"  ID utilisateur: {UserId}\n";
                output += $"  Email stocké: {StoredEmail}\n";
                output += $"  Longueur du mot de passe: {PasswordLength}\n";
                output += $"  Hachage du mot de passe: {PasswordHashType}\n";
                output += $"  Est vendeur: {(IsSeller ? "? Oui" : "? Non")}\n";
                
                if (IsSeller)
                {
                    output += $"  Entreprise: {SellerCompany}\n";
                    output += $"  Certifié: {(IsCertified ? "? Oui" : "? Non")}\n";
                    output += $"  Correspondance du mot de passe: {(PasswordMatches ? "? Oui" : "? Non")}\n";
                    
                    if (!PasswordMatches && PasswordComparison != null)
                    {
                        output += $"  {PasswordComparison}\n";
                    }
                }
            }
            else if (AvailableEmails != null && AvailableEmails.Count > 0)
            {
                output += $"\n  Emails disponibles dans la BD (premiers 10):\n";
                foreach (var e in AvailableEmails)
                {
                    output += $"    - {e}\n";
                }
            }

            output += $"\nRÉSULTAT: {(LoginSuccess ? "? LA CONNEXION DEVRAIT RÉUSSIR" : $"? {FailureReason}")}";
            
            if (ExceptionDetails != null)
            {
                output += $"\n\nDétails de l'exception:\n{ExceptionDetails}";
            }

            return output;
        }
    }
}
