using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;

namespace GroupeV
{
    /// <summary>
    /// Helper class for database operations and health checks
    /// </summary>
    public static class DatabaseHelper
    {
        private const string ConnectionString = "Server=localhost;Uid=root;Pwd=;Database=vente_groupe;Connection Timeout=10;Default Command Timeout=30;";

        /// <summary>
        /// Check if database connection is available
        /// </summary>
        public static async Task<(bool success, string message)> CheckConnectionAsync()
        {
            try
            {
                using var connection = new MySqlConnection(ConnectionString);
                await connection.OpenAsync();
                
                // Verify we can actually query the database
                using var command = new MySqlCommand("SELECT 1", connection);
                await command.ExecuteScalarAsync();
                
                return (true, "Database connection successful");
            }
            catch (MySqlException ex)
            {
                var errorDetail = ex.Number switch
                {
                    0 => "Unable to connect to MySQL server. Please verify:\n  • MySQL/XAMPP is running\n  • Server is accessible on localhost:3306",
                    1042 => "Cannot resolve the database host address",
                    1045 => "Access denied. Check MySQL username and password in connection string",
                    1049 => "Database 'vente_groupe' does not exist",
                    _ => $"MySQL Error {ex.Number}: {ex.Message}"
                };
                return (false, errorDetail);
            }
            catch (System.Net.Sockets.SocketException sockEx)
            {
                return (false, $"Network Error: Cannot reach database server.\n{sockEx.Message}");
            }
            catch (TimeoutException)
            {
                return (false, "Connection timeout. Database server is not responding.");
            }
            catch (Exception ex)
            {
                return (false, $"Connection Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Check if required tables exist in database
        /// </summary>
        public static async Task<(bool success, string message)> VerifyTablesAsync()
        {
            try
            {
                using var connection = new MySqlConnection(ConnectionString);
                await connection.OpenAsync();

                // Check required tables for seller application
                var requiredTables = new[] { "utilisateur", "vendeur", "produit", "categorie", "prevente" };
                
                foreach (var tableName in requiredTables)
                {
                    var exists = await TableExistsAsync(connection, tableName);
                    if (!exists)
                    {
                        return (false, $"Table '{tableName}' doesn't exist. Please ensure the database is set up correctly.");
                    }
                }

                return (true, "All required tables exist");
            }
            catch (Exception ex)
            {
                return (false, $"Table verification error: {ex.Message}");
            }
        }

        /// <summary>
        /// Check if a specific table exists
        /// </summary>
        private static async Task<bool> TableExistsAsync(MySqlConnection connection, string tableName)
        {
            var query = "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = 'vente_groupe' AND table_name = @tableName";
            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@tableName", tableName);

            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result) > 0;
        }

        /// <summary>
        /// Get database statistics
        /// </summary>
        public static async Task<DatabaseStats> GetStatsAsync()
        {
            try
            {
                using var context = new DatabaseContext();

                var sellerCount = await context.Vendeurs.CountAsync();
                var productCount = await context.Produits.CountAsync();
                var categoryCount = await context.Categories.CountAsync();
                var preventeCount = await context.Preventes.CountAsync();

                return new DatabaseStats
                {
                    SellerCount = sellerCount,
                    ProductCount = productCount,
                    CategoryCount = categoryCount,
                    PreventeCount = preventeCount,
                    Success = true
                };
            }
            catch (Exception ex)
            {
                return new DatabaseStats
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        /// <summary>
        /// Ensure database and tables are ready
        /// </summary>
        public static async Task<(bool success, string message)> EnsureDatabaseReadyAsync()
        {
            // Step 1: Check connection
            var connectionResult = await CheckConnectionAsync();
            if (!connectionResult.success)
            {
                return (false, $"Database connection failed:\n\n{connectionResult.message}\n\nPlease ensure:\n1. XAMPP MySQL is running\n2. MySQL service is started on localhost");
            }

            // Step 2: Verify tables
            var tablesResult = await VerifyTablesAsync();
            if (!tablesResult.success)
            {
                return (false, $"Database tables missing:\n\n{tablesResult.message}\n\nPlease:\n1. Open phpMyAdmin (http://localhost/phpmyadmin)\n2. Run the database_setup.sql script");
            }

            // Step 3: Test data access
            try
            {
                var stats = await GetStatsAsync();
                if (!stats.Success)
                {
                    return (false, $"Database access error:\n\n{stats.ErrorMessage}");
                }

                return (true, $"Database ready: {stats.SellerCount} sellers, {stats.ProductCount} products");
            }
            catch (Exception ex)
            {
                return (false, $"Database validation error:\n\n{ex.Message}");
            }
        }
    }

    /// <summary>
    /// Database statistics data class
    /// </summary>
    public class DatabaseStats
    {
        public int SellerCount { get; set; }
        public int ProductCount { get; set; }
        public int CategoryCount { get; set; }
        public int PreventeCount { get; set; }
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
    }
}
