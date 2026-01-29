using System;
using System.Threading.Tasks;

namespace GroupeV
{
    /// <summary>
    /// Quick database test utility - run this to verify your data
    /// Add this as a startup option or call from a button
    /// </summary>
    public static class QuickDbTest
    {
        public static async Task RunTestAsync()
        {
            Console.WriteLine("=== DATABASE CONNECTION TEST ===\n");

            // Test 1: Connection
            Console.WriteLine("Testing database connection...");
            var (success, message) = await DatabaseHelper.CheckConnectionAsync();
            Console.WriteLine($"Result: {(success ? "SUCCESS" : "FAILED")}");
            Console.WriteLine($"Message: {message}\n");

            if (!success)
            {
                Console.WriteLine("Cannot proceed without database connection.");
                return;
            }

            // Test 2: List all sellers
            Console.WriteLine("=== LISTING ALL SELLERS ===\n");
            var sellers = await LoginDiagnostics.ListAllSellersAsync();
            Console.WriteLine(sellers);

            // Test 3: Prompt for test login
            Console.WriteLine("\n=== TEST LOGIN ===");
            Console.Write("Enter email to test: ");
            var email = Console.ReadLine()?.Trim() ?? "";
            
            Console.Write("Enter password to test: ");
            var password = Console.ReadLine()?.Trim() ?? "";

            if (!string.IsNullOrEmpty(email) && !string.IsNullOrEmpty(password))
            {
                Console.WriteLine("\nRunning diagnostics...\n");
                var diagnostic = await LoginDiagnostics.DiagnoseLoginAsync(email, password);
                Console.WriteLine(diagnostic.ToString());
            }

            Console.WriteLine("\n\nPress any key to exit...");
            Console.ReadKey();
        }
    }
}
