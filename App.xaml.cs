using System;
using System.Windows;
using System.Windows.Threading;
using GroupeV.Utilities;

namespace GroupeV
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Global exception handlers
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            DispatcherUnhandledException += App_DispatcherUnhandledException;

            // Démarrer avec l'écran de démarrage chaotique
            var splashScreen = new SplashScreen();
            splashScreen.Show();
        }

        /// <summary>
        /// Optional startup database check
        /// Uncomment the call in OnStartup to enable
        /// </summary>
        private async void CheckDatabaseConnectionAtStartup()
        {
            try
            {
                var (connected, message, stats) = await DatabaseTester.GetConnectionStatusAsync();
                
                if (!connected)
                {
                    var result = MessageBox.Show(
                        $"Database connection failed:\n\n{message}\n\n" +
                        "The application will start but some features may not work.\n\n" +
                        "Would you like to see detailed diagnostics?",
                        "Database Warning",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning);

                    if (result == MessageBoxResult.Yes)
                    {
                        await DatabaseTester.AfficherDiagnosticAsync();
                    }
                }
                else
                {
                    Console.WriteLine($"✓ Database connected: {stats?.ProductCount ?? 0} products available");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Startup database check failed: {ex.Message}");
            }
        }

        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            var errorMessage = $"An unexpected error occurred:\n\n{e.Exception.Message}\n\nStack Trace:\n{e.Exception.StackTrace}";

            MessageBox.Show(
                errorMessage,
                "Application Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            e.Handled = true;
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var exception = e.ExceptionObject as Exception;
            var errorMessage = exception != null
                ? $"A fatal error occurred:\n\n{exception.Message}"
                : "A fatal error occurred.";

            MessageBox.Show(
                errorMessage,
                "Fatal Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }
}
