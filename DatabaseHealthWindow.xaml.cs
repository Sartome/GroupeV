using System;
using System.Threading.Tasks;
using System.Windows;

namespace GroupeV
{
    public partial class DatabaseHealthWindow : Window
    {
        public DatabaseHealthWindow()
        {
            InitializeComponent();
            Loaded += DatabaseHealthWindow_Loaded;
        }

        private async void DatabaseHealthWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await RunHealthCheckAsync();
        }

        private async void RunCheck_Click(object sender, RoutedEventArgs e)
        {
            StatusText.Text = "[SYSTEM] Initiating health check...\n";
            await RunHealthCheckAsync();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private async Task RunHealthCheckAsync()
        {
            AppendStatus("[SYSTEM] ========================================");
            AppendStatus("[SYSTEM] DATABASE HEALTH CHECK STARTED");
            AppendStatus("[SYSTEM] ========================================\n");

            await Task.Delay(300);

            // Check 1: Database Connection
            AppendStatus("[TEST 1/4] Checking database connection...");
            await Task.Delay(500);

            var connectionResult = await DatabaseHelper.CheckConnectionAsync();
            if (connectionResult.success)
            {
                AppendStatus("[OK] ? Database connection successful");
                AppendStatus($"[INFO] {connectionResult.message}\n");
            }
            else
            {
                AppendStatus("[FAIL] ? Database connection failed");
                AppendStatus($"[ERROR] {connectionResult.message}\n");
                AppendStatus("[SYSTEM] Health check aborted due to connection failure\n");
                return;
            }

            await Task.Delay(300);

            // Check 2: Table Verification
            AppendStatus("[TEST 2/4] Verifying database tables...");
            await Task.Delay(500);

            var tablesResult = await DatabaseHelper.VerifyTablesAsync();
            if (tablesResult.success)
            {
                AppendStatus("[OK] ? All required tables exist");
                AppendStatus($"[INFO] {tablesResult.message}\n");
            }
            else
            {
                AppendStatus("[FAIL] ? Table verification failed");
                AppendStatus($"[ERROR] {tablesResult.message}\n");
                AppendStatus("[SYSTEM] Health check aborted due to missing tables\n");
                return;
            }

            await Task.Delay(300);

            // Check 3: Data Statistics
            AppendStatus("[TEST 3/4] Retrieving database statistics...");
            await Task.Delay(500);

            var stats = await DatabaseHelper.GetStatsAsync();
            if (stats.Success)
            {
                AppendStatus("[OK] ? Statistics retrieved successfully");
                AppendStatus($"[DATA] Sellers: {stats.SellerCount}");
                AppendStatus($"[DATA] Products: {stats.ProductCount}");
                AppendStatus($"[DATA] Categories: {stats.CategoryCount}");
                AppendStatus($"[DATA] Pre-sales: {stats.PreventeCount}\n");
            }
            else
            {
                AppendStatus("[FAIL] ? Failed to retrieve statistics");
                AppendStatus($"[ERROR] {stats.ErrorMessage}\n");
            }

            await Task.Delay(300);

            // Check 4: Overall System Status
            AppendStatus("[TEST 4/4] Verifying overall system status...");
            await Task.Delay(500);

            var overallResult = await DatabaseHelper.EnsureDatabaseReadyAsync();
            if (overallResult.success)
            {
                AppendStatus("[OK] ? System is fully operational");
                AppendStatus($"[INFO] {overallResult.message}\n");
            }
            else
            {
                AppendStatus("[FAIL] ? System check failed");
                AppendStatus($"[ERROR] {overallResult.message}\n");
            }

            await Task.Delay(300);

            // Summary
            AppendStatus("[SYSTEM] ========================================");
            if (connectionResult.success && tablesResult.success && stats.Success && overallResult.success)
            {
                AppendStatus("[RESULT] ? ALL SYSTEMS OPERATIONAL");
                AppendStatus("[STATUS] Database is ready for production use");
            }
            else
            {
                AppendStatus("[RESULT] ? ISSUES DETECTED");
                AppendStatus("[STATUS] Please resolve errors before using the application");
            }
            AppendStatus("[SYSTEM] ========================================");
        }

        private void AppendStatus(string message)
        {
            Dispatcher.Invoke(() =>
            {
                StatusText.Text += message + "\n";
            });
        }
    }
}
