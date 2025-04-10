using System;
using System.Data;
using System.Diagnostics;
using System.Windows;
using MySql.Data.MySqlClient;

namespace DataGridNamespace.Utilities
{
    /// <summary>
    /// Utility class for testing and validating database connections
    /// </summary>
    public static class DatabaseTest
    {
        /// <summary>
        /// Tests the database connection and reports any issues
        /// </summary>
        /// <returns>True if the connection is working properly</returns>
        public static bool ValidateConnection()
        {
            try
            {
                using (var connection = DatabaseConnection.GetConnection())
                {
                    if (connection == null || connection.State != ConnectionState.Open)
                    {
                        PlatformUtility.LogError("Database connection could not be established");
                        MessageBox.Show(
                            "Cannot connect to the database. Please check your network connection and try again.",
                            "Connection Error",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                        return false;
                    }

                    return true;
                }
            }
            catch (MySqlException ex)
            {
                PlatformUtility.LogError($"MySQL Error: {ex.Message}", ex);
                
                string message = "Database connection error: ";
                
                switch (ex.Number)
                {
                    case 0:
                        message += "Cannot connect to server.";
                        break;
                    case 1045:
                        message += "Invalid username/password.";
                        break;
                    default:
                        message += ex.Message;
                        break;
                }
                
                MessageBox.Show(
                    message,
                    "Database Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                
                return false;
            }
            catch (Exception ex)
            {
                PlatformUtility.LogError($"General Error: {ex.Message}", ex);
                MessageBox.Show(
                    $"An error occurred: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return false;
            }
        }
        
        /// <summary>
        /// Verifies that the required tables exist in the database
        /// </summary>
        public static bool ValidateTables()
        {
            try
            {
                string[] requiredTables = { "users", "theses", "favoris" };
                bool allTablesExist = true;
                
                using (var connection = DatabaseConnection.GetConnection())
                {
                    if (connection == null || connection.State != ConnectionState.Open)
                    {
                        return false;
                    }
                    
                    foreach (string table in requiredTables)
                    {
                        string query = "SELECT COUNT(*) FROM information_schema.tables " +
                                      "WHERE table_schema = DATABASE() AND table_name = @TableName";
                        using (var cmd = new MySqlCommand(query, connection))
                        {
                            cmd.Parameters.AddWithValue("@TableName", table);
                            int count = Convert.ToInt32(cmd.ExecuteScalar());
                            
                            if (count == 0)
                            {
                                Debug.WriteLine($"Table {table} does not exist in the database");
                                allTablesExist = false;
                            }
                        }
                    }
                }
                
                return allTablesExist;
            }
            catch (Exception ex)
            {
                PlatformUtility.LogError("Error validating database tables", ex);
                return false;
            }
        }
    }
}
