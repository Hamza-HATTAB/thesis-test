using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace DataGridNamespace
{
    /// <summary>
    /// Main application class with global exception handling for increased robustness
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// Application startup point with global exception handling setup
        /// </summary>
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            // Set up global exception handling
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            this.DispatcherUnhandledException += App_DispatcherUnhandledException;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
            
            // Ensure database directory exists
            EnsureDatabaseConnectivity();
        }
        
        /// <summary>
        /// Validates basic database connectivity
        /// </summary>
        private void EnsureDatabaseConnectivity()
        {
            try
            {
                // Test database connection on startup
                using (var connection = DatabaseConnection.GetConnection())
                {
                    if (connection != null && connection.State == ConnectionState.Open)
                    {
                        Debug.WriteLine("Database connection successful on application startup");
                    }
                    else
                    {
                        LogError("Database connection could not be established on startup");
                    }
                }
            }
            catch (Exception ex)
            {
                LogError($"Database initialization error: {ex.Message}");
                // Continue application startup despite database errors
                // The user will be informed about connection issues when they try to access database features
            }
        }

        /// <summary>
        /// Handler for dispatcher (UI thread) unhandled exceptions
        /// </summary>
        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            try
            {
                e.Handled = true; // Prevent application crash
                string errorMessage = $"An unexpected UI error occurred: {e.Exception.Message}";
                LogError(errorMessage, e.Exception);
                MessageBox.Show(errorMessage + "\n\nThe error has been logged. Please restart the application if you encounter further issues.", 
                               "Application Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch
            {
                // If we can't handle the exception properly, let it propagate
                e.Handled = false;
            }
        }

        /// <summary>
        /// Handler for non-UI thread unhandled exceptions
        /// </summary>
        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            try
            {
                var exception = e.ExceptionObject as Exception;
                string errorMessage = exception != null ? 
                    $"A critical error occurred: {exception.Message}" : 
                    "A critical error occurred";
                
                LogError(errorMessage, exception);
                MessageBox.Show(errorMessage + "\n\nThe application may become unstable. Please save your work and restart the application.", 
                               "Critical Application Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch
            {
                // Last resort if the error handling itself fails
            }
        }

        /// <summary>
        /// Handler for unobserved task exceptions
        /// </summary>
        private void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            try
            {
                e.SetObserved(); // Mark as observed to prevent application crash
                LogError($"Background task error: {e.Exception.Message}", e.Exception);
            }
            catch
            {
                // Last resort if the error handling itself fails
            }
        }

        /// <summary>
        /// Log errors to file and debug output
        /// </summary>
        private void LogError(string message, Exception ex = null)
        {
            try
            {
                // Log to debug output
                Debug.WriteLine($"[ERROR] {DateTime.Now}: {message}");
                if (ex != null)
                {
                    Debug.WriteLine($"Exception details: {ex.Message}");
                    Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                }
                
                // Log to file
                string logFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
                if (!Directory.Exists(logFolder))
                {
                    Directory.CreateDirectory(logFolder);
                }
                
                string logFile = Path.Combine(logFolder, $"ErrorLog_{DateTime.Now:yyyy-MM-dd}.txt");
                using (StreamWriter writer = File.AppendText(logFile))
                {
                    writer.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}");
                    if (ex != null)
                    {
                        writer.WriteLine($"Exception: {ex.GetType().Name}");
                        writer.WriteLine($"Message: {ex.Message}");
                        writer.WriteLine($"Stack trace: {ex.StackTrace}");
                        writer.WriteLine(new string('-', 80));
                    }
                }
            }
            catch
            {
                // Silently fail if logging itself fails
            }
        }
    }
}
