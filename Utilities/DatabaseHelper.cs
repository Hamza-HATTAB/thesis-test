using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using MySql.Data.MySqlClient;

namespace DataGridNamespace.Utilities
{
    public static class DatabaseHelper
    {
        /// <summary>
        /// Executes a query that returns a result set and properly closes the connection.
        /// </summary>
        public static DataTable ExecuteQuerySafe(string query, Dictionary<string, object> parameters = null)
        {
            MySqlConnection connection = null;
            try
            {
                connection = DatabaseConnection.GetConnection();
                using (var command = new MySqlCommand(query, connection))
                {
                    // Add parameters if provided
                    if (parameters != null)
                    {
                        foreach (var param in parameters)
                        {
                            command.Parameters.AddWithValue(param.Key, param.Value);
                        }
                    }

                    using (var adapter = new MySqlDataAdapter(command))
                    {
                        var dataTable = new DataTable();
                        adapter.Fill(dataTable);
                        return dataTable;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Database Error #{ex.HResult}: {ex.Message}");
                return null;
            }
            finally
            {
                // Always close the connection
                if (connection != null && connection.State != ConnectionState.Closed)
                {
                    connection.Close();
                }
            }
        }

        /// <summary>
        /// Executes a scalar query and properly closes the connection.
        /// </summary>
        public static object ExecuteScalarSafe(string query, Dictionary<string, object> parameters = null)
        {
            MySqlConnection connection = null;
            try
            {
                connection = DatabaseConnection.GetConnection();
                using (var command = new MySqlCommand(query, connection))
                {
                    // Add parameters if provided
                    if (parameters != null)
                    {
                        foreach (var param in parameters)
                        {
                            command.Parameters.AddWithValue(param.Key, param.Value);
                        }
                    }

                    return command.ExecuteScalar();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Database Error #{ex.HResult}: {ex.Message}");
                return null;
            }
            finally
            {
                // Always close the connection
                if (connection != null && connection.State != ConnectionState.Closed)
                {
                    connection.Close();
                }
            }
        }

        /// <summary>
        /// Executes a non-query command and properly closes the connection.
        /// </summary>
        public static int ExecuteNonQuerySafe(string query, Dictionary<string, object> parameters = null)
        {
            MySqlConnection connection = null;
            try
            {
                connection = DatabaseConnection.GetConnection();
                using (var command = new MySqlCommand(query, connection))
                {
                    // Add parameters if provided
                    if (parameters != null)
                    {
                        foreach (var param in parameters)
                        {
                            command.Parameters.AddWithValue(param.Key, param.Value);
                        }
                    }

                    return command.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Database Error #{ex.HResult}: {ex.Message}");
                return -1;
            }
            finally
            {
                // Always close the connection
                if (connection != null && connection.State != ConnectionState.Closed)
                {
                    connection.Close();
                }
            }
        }
    }
}