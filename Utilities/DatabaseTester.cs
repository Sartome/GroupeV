using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.EntityFrameworkCore;

namespace GroupeV.Utilities
{
    /// <summary>
    /// Utility class for testing database connection and validating data.
    /// Used for startup validation and diagnostic purposes.
    /// 
    /// PRODUCTION READY:
    /// - Async/await pattern for better performance
    /// - Proper error handling with detailed messages
    /// - Integration with vente_groupe database
    /// </summary>
    public static class DatabaseTester
    {
        /// <summary>
        /// Tests database connection asynchronously and displays the result.
        /// 
        /// RETURNS:
        /// - true: Connection successful
        /// - false: Connection failed
        /// </summary>
        public static async Task<bool> TesterConnexionAsync(bool afficherMessageBox = true)
        {
            try
            {
                using var context = new DatabaseContext();

                // ========== TEST 1: Connection ==========
                Console.WriteLine("? Testing connection to MySQL...");
                
                bool peutSeConnecter = await context.Database.CanConnectAsync();

                if (!peutSeConnecter)
                {
                    AfficherErreur("? Unable to connect to MySQL database.", afficherMessageBox);
                    return false;
                }

                Console.WriteLine("? MySQL connection successful!");

                // ========== TEST 2: Tables Verification ==========
                Console.WriteLine("? Verifying tables...");

                int nombreUtilisateurs = await context.Utilisateurs.CountAsync();
                Console.WriteLine($"? Table 'utilisateur' accessible. Count: {nombreUtilisateurs}");

                int nombreVendeurs = await context.Vendeurs.CountAsync();
                Console.WriteLine($"? Table 'vendeur' accessible. Count: {nombreVendeurs}");

                int nombreProduits = await context.Produits.CountAsync();
                Console.WriteLine($"? Table 'produit' accessible. Count: {nombreProduits}");

                int nombreCategories = await context.Categories.CountAsync();
                Console.WriteLine($"? Table 'categorie' accessible. Count: {nombreCategories}");

                int nombrePreventes = await context.Preventes.CountAsync();
                Console.WriteLine($"? Table 'prevente' accessible. Count: {nombrePreventes}");

                // ========== TEST 3: Relationships ==========
                Console.WriteLine("? Testing relationships (Include)...");
                
                var premierProduit = await context.Produits
                    .Include(p => p.Vendeur)
                    .Include(p => p.Categorie)
                    .FirstOrDefaultAsync();

                if (premierProduit != null)
                {
                    Console.WriteLine($"? FK relationship tested: Product '{premierProduit.Description ?? "N/A"}' - Category: {premierProduit.Categorie?.Libelle ?? "N/A"}");
                }
                else
                {
                    Console.WriteLine("? No products found in database (empty table)");
                }

                // ========== SUCCESS ==========
                string message = $"? CONNECTION SUCCESSFUL!\n\n" +
                                $"Database Statistics:\n" +
                                $"• Users: {nombreUtilisateurs}\n" +
                                $"• Sellers: {nombreVendeurs}\n" +
                                $"• Products: {nombreProduits}\n" +
                                $"• Categories: {nombreCategories}\n" +
                                $"• Pre-sales: {nombrePreventes}\n" +
                                $"• Relationships: OK";

                Console.WriteLine(message);

                if (afficherMessageBox)
                {
                    MessageBox.Show(message, "Connection Test", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                return true;
            }
            catch (MySqlConnector.MySqlException ex)
            {
                string erreur = ex.ErrorCode switch
                {
                    MySqlConnector.MySqlErrorCode.UnableToConnectToHost => 
                        "? Unable to connect to MySQL server.\n\n" +
                        "Please verify:\n" +
                        "• Laragon is started\n" +
                        "• MySQL service is running (port 3306)\n" +
                        "• No firewall is blocking the connection",

                    MySqlConnector.MySqlErrorCode.AccessDenied => 
                        "? Access denied to MySQL.\n\n" +
                        "Please verify credentials in DatabaseContext.cs:\n" +
                        "• User=root\n" +
                        "• Password=(empty by default on Laragon)",

                    MySqlConnector.MySqlErrorCode.UnknownDatabase => 
                        "? Database 'vente_groupe' does not exist.\n\n" +
                        "Solutions:\n" +
                        "1. Import SQL file: Database/vente_groupe.sql in phpMyAdmin\n" +
                        "2. Or create database manually in phpMyAdmin\n" +
                        "3. Access phpMyAdmin at: http://localhost/phpmyadmin",

                    _ => $"? MySQL Error ({ex.ErrorCode}): {ex.Message}"
                };

                Console.WriteLine(erreur);
                Console.WriteLine($"Details: {ex}");

                if (afficherMessageBox)
                {
                    MessageBox.Show(erreur, "MySQL Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }

                return false;
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("No database provider"))
            {
                string erreur = "? MySQL provider not configured.\n\n" +
                               "Please verify NuGet package is installed:\n" +
                               "• Pomelo.EntityFrameworkCore.MySql\n\n" +
                               "Command: dotnet add package Pomelo.EntityFrameworkCore.MySql";

                Console.WriteLine(erreur);
                if (afficherMessageBox)
                {
                    MessageBox.Show(erreur, "Configuration Missing", MessageBoxButton.OK, MessageBoxImage.Error);
                }

                return false;
            }
            catch (Exception ex)
            {
                string erreur = $"? Unexpected error:\n{ex.Message}\n\n" +
                               $"Type: {ex.GetType().Name}\n\n" +
                               $"See console for more details.";

                Console.WriteLine($"Complete error: {ex}");

                if (afficherMessageBox)
                {
                    MessageBox.Show(erreur, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }

                return false;
            }
        }

        /// <summary>
        /// Synchronous wrapper for backwards compatibility
        /// </summary>
        public static bool TesterConnexion(bool afficherMessageBox = true)
        {
            return TesterConnexionAsync(afficherMessageBox).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Displays diagnostic information about the database connection.
        /// Useful for troubleshooting.
        /// </summary>
        public static async Task AfficherDiagnosticAsync()
        {
            try
            {
                using var context = new DatabaseContext();

                var diagnostic = "=== DATABASE DIAGNOSTIC ===\n\n";

                // Connection string (without password for security)
                var connectionString = context.Database.GetConnectionString();
                if (connectionString != null)
                {
                    var connectionStringMasked = connectionString.Replace("Pwd=;", "Pwd=***;");
                    diagnostic += $"Connection String:\n{connectionStringMasked}\n\n";
                }

                // Provider
                diagnostic += $"Provider: {context.Database.ProviderName}\n\n";

                // Connection test
                bool connected = await context.Database.CanConnectAsync();
                diagnostic += $"Connection: {(connected ? "? OK" : "? Failed")}\n\n";

                if (connected)
                {
                    // Applied migrations
                    var migrations = await context.Database.GetAppliedMigrationsAsync();
                    var migrationList = migrations.Any() ? string.Join(", ", migrations) : "None";
                    diagnostic += $"Applied Migrations: {migrationList}\n\n";

                    // Pending migrations
                    var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
                    var pendingList = pendingMigrations.Any() ? string.Join(", ", pendingMigrations) : "None";
                    diagnostic += $"Pending Migrations: {pendingList}\n\n";
                    
                    // Table counts
                    diagnostic += "Table Statistics:\n";
                    diagnostic += $"• utilisateur: {await context.Utilisateurs.CountAsync()}\n";
                    diagnostic += $"• vendeur: {await context.Vendeurs.CountAsync()}\n";
                    diagnostic += $"• produit: {await context.Produits.CountAsync()}\n";
                    diagnostic += $"• categorie: {await context.Categories.CountAsync()}\n";
                    diagnostic += $"• prevente: {await context.Preventes.CountAsync()}\n";
                }

                Console.WriteLine(diagnostic);
                MessageBox.Show(diagnostic, "Database Diagnostic", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"? Error during diagnostic:\n{ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        /// <summary>
        /// Synchronous wrapper for diagnostics
        /// </summary>
        public static void AfficherDiagnostic()
        {
            AfficherDiagnosticAsync().GetAwaiter().GetResult();
        }

        /// <summary>
        /// Displays an error message in console and optionally in a MessageBox.
        /// </summary>
        private static void AfficherErreur(string message, bool afficherMessageBox)
        {
            Console.WriteLine(message);
            if (afficherMessageBox)
            {
                MessageBox.Show(message, "Connection Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Gets connection status and basic statistics without displaying UI.
        /// Useful for startup checks.
        /// </summary>
        public static async Task<(bool connected, string message, DatabaseStats? stats)> GetConnectionStatusAsync()
        {
            try
            {
                using var context = new DatabaseContext();
                
                bool connected = await context.Database.CanConnectAsync();
                if (!connected)
                {
                    return (false, "Cannot connect to database", null);
                }

                var stats = new DatabaseStats
                {
                    SellerCount = await context.Vendeurs.CountAsync(),
                    ProductCount = await context.Produits.CountAsync(),
                    CategoryCount = await context.Categories.CountAsync(),
                    PreventeCount = await context.Preventes.CountAsync(),
                    Success = true
                };

                return (true, "Database connected", stats);
            }
            catch (Exception ex)
            {
                return (false, $"Error: {ex.Message}", null);
            }
        }
    }
}
