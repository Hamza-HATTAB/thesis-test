using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using MySql.Data.MySqlClient;
using System.Windows;

namespace DataGridNamespace
{
    /// <summary>
    /// Centralized database connection management class
    /// </summary>
    public static class DatabaseConnection
    {
        // Connection string for database access
        private static string connectionString = "Server=localhost;Database=gestion_theses;User ID=root;Password=";
        
        // Static connection instance
        private static MySqlConnection connection = null;

        /// <summary>
        /// Gets a connection to the database, creating a new one if necessary
        /// </summary>
        public static MySqlConnection GetConnection()
        {
            try
            {
                if (connection == null || connection.State == ConnectionState.Closed)
                {
                    connection = new MySqlConnection(connectionString);
                    connection.Open();
                }
                return connection;
            }
            catch (MySqlException ex)
            {
                // Log the error
                Debug.WriteLine($"Database Error #{ex.Number}: {ex.Message}");
                
                // Handle specific error cases
                HandleDatabaseError(ex);
                
                throw; // Re-throw to let the calling code handle it
            }
        }

        /// <summary>
        /// Tests the database connection and returns the result
        /// </summary>
        /// <returns>True if the connection is successful</returns>
        public static bool TestConnection()
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    Console.WriteLine("Connexion réussie !");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur de connexion : {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Executes a query that returns a single value
        /// </summary>
        public static object ExecuteScalar(string query, Dictionary<string, object> parameters = null)
        {
            using (var connection = GetConnection())
            {
                using (var command = new MySqlCommand(query, connection))
                {
                    try
                    {
                        // Add parameters if they exist
                        if (parameters != null)
                        {
                            foreach (var param in parameters)
                            {
                                command.Parameters.AddWithValue(param.Key, param.Value);
                            }
                        }

                        return command.ExecuteScalar();
                    }
                    catch (MySqlException ex)
                    {
                        HandleDatabaseError(ex);
                        throw;
                    }
                }
            }
        }

        /// <summary>
        /// Executes a query that doesn't return any results
        /// </summary>
        public static int ExecuteNonQuery(string query, Dictionary<string, object> parameters = null)
        {
            using (var connection = GetConnection())
            {
                using (var command = new MySqlCommand(query, connection))
                {
                    try
                    {
                        // Add parameters if they exist
                        if (parameters != null)
                        {
                            foreach (var param in parameters)
                            {
                                command.Parameters.AddWithValue(param.Key, param.Value);
                            }
                        }

                        return command.ExecuteNonQuery();
                    }
                    catch (MySqlException ex)
                    {
                        HandleDatabaseError(ex);
                        throw;
                    }
                }
            }
        }

        /// <summary>
        /// Executes a query and returns a MySqlDataReader
        /// </summary>
        public static MySqlDataReader ExecuteReader(string query, Dictionary<string, object> parameters = null)
        {
            var connection = GetConnection();
            
            var command = new MySqlCommand(query, connection);
            
            try
            {
                // Add parameters if they exist
                if (parameters != null)
                {
                    foreach (var param in parameters)
                    {
                        command.Parameters.AddWithValue(param.Key, param.Value);
                    }
                }

                // CommandBehavior.CloseConnection will close the connection when the reader is closed
                return command.ExecuteReader(CommandBehavior.CloseConnection);
            }
            catch (MySqlException ex)
            {
                // Safely close connection if it exists and is open
                if (connection != null && connection.State == ConnectionState.Open)
                {
                    connection.Close();
                }
                
                HandleDatabaseError(ex);
                throw;
            }
        }

        /// <summary>
        /// Handles database errors
        /// </summary>
        private static void HandleDatabaseError(MySqlException ex)
        {
            string errorMessage;
            
            switch (ex.Number)
            {
                case 0:
                    errorMessage = "Cannot connect to database server.";
                    break;
                case 1042:
                    errorMessage = "Unable to connect to MySQL server.";
                    break;
                case 1045:
                    errorMessage = "Invalid database credentials.";
                    break;
                default:
                    errorMessage = $"Database error: {ex.Message}";
                    break;
            }
            
            Debug.WriteLine($"Database Error #{ex.Number}: {ex.Message}");
            
            if (Application.Current != null)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show(errorMessage, "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
        }
    }
}