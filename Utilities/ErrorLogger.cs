using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace DataGridNamespace.Utilities
{
    /// <summary>
    /// Comprehensive error logging utility with database and file logging capabilities
    /// </summary>
    public static class ErrorLogger
    {
        private static readonly string LogFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
        private static bool _tablePrepared = false;
        
        /// <summary>
        /// Gets whether the ErrorLogger has been initialized
        /// </summary>
        public static bool IsInitialized { get; private set; } = true;
        
        /// <summary>
        /// Log an error to both database and file
        /// </summary>
        public static void LogError(string message, Exception ex = null, bool showMessageBox = false)
        {
            string errorDetails = ex != null ? $"{ex.GetType().Name}: {ex.Message}" : "No exception details";
            string stackTrace = ex != null ? ex.StackTrace : "No stack trace";
            
            // Always log to debug output
            Debug.WriteLine($"[ERROR] {DateTime.Now}: {message}");
            Debug.WriteLine($"Details: {errorDetails}");
            if (ex != null)
            {
                Debug.WriteLine($"Stack trace: {stackTrace}");
            }
            
            // Log to file
            LogToFile(message, errorDetails, stackTrace);
            
            // Log to database
            LogToDatabase(message, errorDetails, stackTrace);
            
            // Show message box if requested
            if (showMessageBox)
            {
                MessageBox.Show(
                    $"{message}\n\nDetails: {errorDetails}",
                    "Application Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
        
        /// <summary>
        /// Log error information to a file
        /// </summary>
        private static void LogToFile(string message, string errorDetails, string stackTrace)
        {
            try
            {
                // Ensure log directory exists
                if (!Directory.Exists(LogFolder))
                {
                    Directory.CreateDirectory(LogFolder);
                }
                
                string logFile = Path.Combine(LogFolder, $"ErrorLog_{DateTime.Now:yyyy-MM-dd}.txt");
                
                // Write to log file
                using (StreamWriter writer = File.AppendText(logFile))
                {
                    writer.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}");
                    writer.WriteLine($"Details: {errorDetails}");
                    writer.WriteLine($"Stack trace: {stackTrace}");
                    writer.WriteLine(new string('-', 80));
                }
            }
            catch (Exception ex)
            {
                // Fallback to debug output if file logging fails
                Debug.WriteLine($"Failed to write to error log file: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Log error information to the database
        /// </summary>
        private static void LogToDatabase(string message, string errorDetails, string stackTrace)
        {
            try
            {
                // Ensure error log table exists
                EnsureErrorLogTableExists();
                
                // Prepare parameters
                var logParams = new Dictionary<string, object>
                {
                    { "@Message", message },
                    { "@ErrorDetails", errorDetails },
                    { "@StackTrace", stackTrace },
                    { "@UserId", Session.IsLoggedIn ? Session.CurrentUserId : (object)DBNull.Value },
                    { "@Username", Session.IsLoggedIn ? Session.CurrentUsername : (object)DBNull.Value },
                    { "@LogTime", DateTime.Now }
                };
                
                // Insert error log
                string insertQuery = @"
                    INSERT INTO error_log 
                    (message, error_details, stack_trace, user_id, username, log_time) 
                    VALUES 
                    (@Message, @ErrorDetails, @StackTrace, @UserId, @Username, @LogTime)";
                
                DatabaseConnection.ExecuteNonQuery(insertQuery, logParams);
            }
            catch (Exception ex)
            {
                // Fallback to debug output if database logging fails
                Debug.WriteLine($"Failed to log to database: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Ensure the error_log table exists in the database
        /// </summary>
        private static void EnsureErrorLogTableExists()
        {
            if (_tablePrepared)
            {
                return;
            }
            
            try
            {
                string createTableQuery = @"
                    CREATE TABLE IF NOT EXISTS error_log (
                        id INT AUTO_INCREMENT PRIMARY KEY,
                        message TEXT NOT NULL,
                        error_details TEXT,
                        stack_trace TEXT,
                        user_id INT,
                        username VARCHAR(100),
                        log_time DATETIME NOT NULL,
                        INDEX idx_log_time (log_time),
                        INDEX idx_user_id (user_id)
                    )";
                
                DatabaseConnection.ExecuteNonQuery(createTableQuery);
                
                // Also create user activity log table if it doesn't exist
                string createActivityLogTableQuery = @"
                    CREATE TABLE IF NOT EXISTS user_activity_log (
                        id INT AUTO_INCREMENT PRIMARY KEY,
                        user_id INT NOT NULL,
                        username VARCHAR(100) NOT NULL,
                        activity_type VARCHAR(50) NOT NULL,
                        activity_time DATETIME NOT NULL,
                        session_token VARCHAR(255),
                        details TEXT,
                        INDEX idx_user_id (user_id),
                        INDEX idx_activity_time (activity_time),
                        INDEX idx_activity_type (activity_type)
                    )";
                
                DatabaseConnection.ExecuteNonQuery(createActivityLogTableQuery);
                
                _tablePrepared = true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to create error log table: {ex.Message}");
                // Continue without database logging
            }
        }
        
        /// <summary>
        /// View the most recent errors from the log
        /// </summary>
        public static List<Dictionary<string, object>> GetRecentErrors(int count = 50)
        {
            try
            {
                EnsureErrorLogTableExists();
                
                string query = @"
                    SELECT id, message, error_details, user_id, username, log_time 
                    FROM error_log 
                    ORDER BY log_time DESC 
                    LIMIT @Count";
                
                var parameters = new Dictionary<string, object>
                {
                    { "@Count", count }
                };
                
                var result = new List<Dictionary<string, object>>();
                
                using (var reader = DatabaseConnection.ExecuteReader(query, parameters))
                {
                    while (reader.Read())
                    {
                        var row = new Dictionary<string, object>
                        {
                            { "Id", reader["id"] },
                            { "Message", reader["message"] },
                            { "ErrorDetails", reader["error_details"] },
                            { "UserId", reader["user_id"] },
                            { "Username", reader["username"] },
                            { "LogTime", reader["log_time"] }
                        };
                        
                        result.Add(row);
                    }
                }
                
                return result;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to retrieve error logs: {ex.Message}");
                return new List<Dictionary<string, object>>();
            }
        }
    }
}
