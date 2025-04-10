using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using MySql.Data.MySqlClient;

namespace DataGridNamespace.Utilities
{
    /// <summary>
    /// Enhanced connection manager for reliable database operations
    /// </summary>
    public static class ConnectionManager
    {
        private static readonly object lockObject = new object();
        private static readonly TimeSpan connectionTimeout = TimeSpan.FromSeconds(15);
        private static readonly int maxRetries = 3;
        private static readonly TimeSpan retryDelay = TimeSpan.FromSeconds(2);
        
        /// <summary>
        /// Executes a database query with automatic retries and robust error handling
        /// </summary>
        // Make sure this method is using the correct DatabaseConnection class
        public static T ExecuteWithRetry<T>(Func<MySqlConnection, T> operation, string operationName = "database operation")
        {
            int attempts = 0;
            Exception lastException = null;
            
            while (attempts < maxRetries)
            {
                attempts++;
                try
                {
                    using (var connection = DatabaseConnection.GetConnection())
                    {
                        if (connection == null || connection.State != ConnectionState.Open)
                        {
                            if (attempts == maxRetries)
                            {
                                throw new InvalidOperationException("Could not establish database connection after multiple attempts");
                            }
                            
                            PlatformUtility.LogError($"Connection attempt {attempts} failed for {operationName}");
                            Thread.Sleep(retryDelay);
                            continue;
                        }
                        
                        // Execute the operation
                        return operation(connection);
                    }
                }
                catch (MySqlException ex) when (IsTransientError(ex) && attempts < maxRetries)
                {
                    // Log the error for diagnostic purposes
                    PlatformUtility.LogError($"Transient error on attempt {attempts} for {operationName}: {ex.Message}", ex);
                    lastException = ex;
                    
                    // Wait before retrying
                    Thread.Sleep(retryDelay);
                }
                catch (Exception ex)
                {
                    // Log non-transient errors or final attempt failures
                    PlatformUtility.LogError($"Error on attempt {attempts} for {operationName}: {ex.Message}", ex);
                    lastException = ex;
                    
                    // Non-transient errors or last attempt, rethrow
                    if (!IsTransientError(ex) || attempts >= maxRetries)
                    {
                        throw;
                    }
                    
                    // Wait before retrying for transient errors
                    Thread.Sleep(retryDelay);
                }
            }
            
            // This should generally not be reached as exceptions should be thrown,
            // but included as a safeguard
            throw new InvalidOperationException(
                $"Failed to complete {operationName} after {maxRetries} attempts", 
                lastException);
        }
        
        /// <summary>
        /// Executes a database query with no return value, with retries
        /// </summary>
        public static void ExecuteNonQueryWithRetry(string query, Dictionary<string, object> parameters = null, string operationName = "non-query operation")
        {
            ExecuteWithRetry(connection => 
            {
                using (var command = new MySqlCommand(query, connection))
                {
                    if (parameters != null)
                    {
                        foreach (var parameter in parameters)
                        {
                            command.Parameters.AddWithValue(parameter.Key, parameter.Value ?? DBNull.Value);
                        }
                    }
                    
                    return command.ExecuteNonQuery();
                }
            }, operationName);
        }
        
        /// <summary>
        /// Executes a database query returning a scalar result, with retries
        /// </summary>
        public static object ExecuteScalarWithRetry(string query, Dictionary<string, object> parameters = null, string operationName = "scalar query")
        {
            return ExecuteWithRetry(connection => 
            {
                using (var command = new MySqlCommand(query, connection))
                {
                    if (parameters != null)
                    {
                        foreach (var parameter in parameters)
                        {
                            command.Parameters.AddWithValue(parameter.Key, parameter.Value ?? DBNull.Value);
                        }
                    }
                    
                    return command.ExecuteScalar();
                }
            }, operationName);
        }
        
        /// <summary>
        /// Executes a database query returning a reader, with retries
        /// </summary>
        public static MySqlDataReader ExecuteReaderWithRetry(string query, Dictionary<string, object> parameters = null, string operationName = "reader query")
        {
            return ExecuteWithRetry(connection => 
            {
                var command = new MySqlCommand(query, connection);
                
                if (parameters != null)
                {
                    foreach (var parameter in parameters)
                    {
                        command.Parameters.AddWithValue(parameter.Key, parameter.Value ?? DBNull.Value);
                    }
                }
                
                // CommandBehavior.CloseConnection ensures the connection is closed when the reader is closed
                return command.ExecuteReader(CommandBehavior.CloseConnection);
            }, operationName);
        }
        
        /// <summary>
        /// Determines if an exception represents a transient database error that may be resolved by retrying
        /// </summary>
        private static bool IsTransientError(Exception ex)
        {
            if (ex is MySqlException mysqlEx)
            {
                // Common MySql error codes that are transient
                switch (mysqlEx.Number)
                {
                    // Connection errors
                    case 1042: // Unable to connect to any of the specified MySQL hosts
                    case 1043: // Bad handshake
                    case 2003: // Connection refused
                    case 2006: // Server has gone away
                    case 2013: // Lost connection during query
                        
                    // Concurrency errors
                    case 1205: // Lock wait timeout exceeded
                    case 1213: // Deadlock found
                        
                    // Resource errors
                    case 1040: // Too many connections
                    case 1203: // User %s already has more than 'max_user_connections' active connections
                        return true;
                }
            }
            
            // Check for other transient error types
            return ex is TimeoutException || 
                   (ex.Message?.Contains("timeout", StringComparison.OrdinalIgnoreCase) ?? false) ||
                   (ex.Message?.Contains("connection reset", StringComparison.OrdinalIgnoreCase) ?? false);
        }
    }
}
