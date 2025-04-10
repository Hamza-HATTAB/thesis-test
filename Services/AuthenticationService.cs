using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography; // Required for hashing and RNG
using System.Text;
using System.Windows; // For MessageBox
using DataGridNamespace; // For DatabaseConnection and Session
using UserModels; // For User class
using System.Diagnostics; // For Debug

namespace DataGridNamespace.Services // Assuming a Services namespace
{
    /// <summary>
    /// Provides services related to user authentication, including login and password management.
    /// </summary>
    public class AuthenticationService
    {
        // Constants for secure password hashing (adjust iterations as needed)
        private const int SaltSize = 16; // 128 bits
        private const int HashSize = 32; // 256 bits
        // Iterations should be high enough to be slow, but not cripple the server/UI.
        // Adjust based on performance testing. 10000 is a bare minimum, >50000 recommended.
        private const int Iterations = 30000; // Increased iterations

        /// <summary>
        /// Attempts to log in a user with the provided credentials.
        /// Uses plain text comparison due to current DB schema. THIS IS INSECURE.
        /// </summary>
        /// <param name="username">The username entered by the user.</param>
        /// <param name="password">The password entered by the user.</param>
        /// <returns>The authenticated User object if login is successful, otherwise null.</returns>
        public User Login(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Username and password cannot be empty.", "Login Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return null;
            }

            try
            {
                // Query to get user details including the PLAIN TEXT password
                string query = "SELECT id, nom, email, role, password FROM users WHERE Nom = @Username";
                var parameters = new Dictionary<string, object> { { "@Username", username } };

                using (var reader = DatabaseConnection.ExecuteReader(query, parameters))
                {
                    if (reader != null && reader.Read())
                    {
                        string storedPlainTextPassword = reader.GetString("password");

                        // --- INSECURE Plain Text Password Comparison ---
                        // WARNING: This method is highly insecure and should be replaced
                        // with hashed password verification as soon as possible.
                        if (password == storedPlainTextPassword)
                        {
                            // --- Login Successful ---
                            int userId = reader.GetInt32("id");
                            string userRole = reader.GetString("role");
                            string userEmail = reader.GetString("email");
                            string userNom = reader.GetString("nom");

                            // Initialize the user session using the correct method
                            Session.InitializeSession(userId, userRole, userNom); // FIXED: Was Session.Login

                            // Success message (optional, can be handled by caller)
                            // MessageBox.Show($"Welcome {userNom}! Login successful.", "Login Success", MessageBoxButton.OK, MessageBoxImage.Information);

                            // Return the user object (without password)
                            return new User
                            {
                                Id = userId,
                                Nom = userNom,
                                Email = userEmail,
                                Role = ConvertStringToRole(userRole) // Use helper for enum conversion
                            };
                        }
                        else
                        {
                            // --- Login Failed (Password Incorrect) ---
                            Debug.WriteLine($"Login failed for user '{username}': Incorrect password.");
                            MessageBox.Show("Invalid username or password.", "Login Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                            // Log failed attempt if needed
                            return null;
                        }
                        // --- End INSECURE block ---
                    }
                    else
                    {
                        // --- Login Failed (User Not Found) ---
                        Debug.WriteLine($"Login failed: User '{username}' not found.");
                        MessageBox.Show("Invalid username or password.", "Login Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                        return null;
                    }
                } // Reader and connection closed automatically
            }
            catch (InvalidOperationException dbEx) // Catch specific connection errors from DatabaseConnection
            {
                MessageBox.Show($"Database connection error during login: {dbEx.Message}", "Login Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Debug.WriteLine($"Login Database Connection Exception: {dbEx}");
                return null;
            }
            catch (Exception ex) // Catch unexpected errors
            {
                MessageBox.Show($"An unexpected error occurred during login: {ex.Message}", "Login Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Debug.WriteLine($"Login Exception: {ex}");
                return null;
            }
        }


        // --- Secure Password Hashing Methods (FOR FUTURE USE) ---
        // These require database columns 'password_hash' (VARCHAR/TEXT) and 'password_salt' (VARCHAR/TEXT)

        /// <summary>
        /// Generates a salt and hashes the password using PBKDF2 with SHA256.
        /// FOR FUTURE USE WHEN DB SCHEMA IS UPDATED.
        /// </summary>
        /// <param name="password">The plain text password.</param>
        /// <param name="salt">Output parameter for the generated salt (Base64 encoded).</param>
        /// <returns>The hashed password (Base64 encoded).</returns>
        public string HashPassword(string password, out string salt)
        {
            // Generate a random salt using a modern cryptographic RNG
            byte[] saltBytes = RandomNumberGenerator.GetBytes(SaltSize); // FIXED: Use modern RNG API
            salt = Convert.ToBase64String(saltBytes); // Store salt as Base64 string

            // Hash the password using PBKDF2 with SHA256 and specified iterations
            // FIXED: Use overload specifying HashAlgorithmName and Iterations
            using (var pbkdf2 = new Rfc2898DeriveBytes(password, saltBytes, Iterations, HashAlgorithmName.SHA256))
            {
                byte[] hashBytes = pbkdf2.GetBytes(HashSize);
                return Convert.ToBase64String(hashBytes); // Return hash as Base64 string
            }
        }

        /// <summary>
        /// Verifies a given password against a stored salt and hash.
        /// FOR FUTURE USE WHEN DB SCHEMA IS UPDATED.
        /// </summary>
        /// <param name="password">The password entered by the user.</param>
        /// <param name="saltBase64">The stored salt (Base64 encoded).</param>
        /// <param name="hashBase64">The stored hash (Base64 encoded).</param>
        /// <returns>True if the password matches the hash, false otherwise.</returns>
        public bool VerifyPassword(string password, string saltBase64, string hashBase64)
        {
            try
            {
                byte[] saltBytes = Convert.FromBase64String(saltBase64);
                byte[] expectedHashBytes = Convert.FromBase64String(hashBase64);

                // Hash the entered password using the *stored* salt and same parameters
                // FIXED: Use overload specifying HashAlgorithmName and Iterations
                using (var pbkdf2 = new Rfc2898DeriveBytes(password, saltBytes, Iterations, HashAlgorithmName.SHA256))
                {
                    byte[] actualHashBytes = pbkdf2.GetBytes(HashSize);

                    // Compare the computed hash with the stored hash using a constant-time comparison
                    // to mitigate timing attacks.
                    return CryptographicOperations.FixedTimeEquals(expectedHashBytes, actualHashBytes);
                }
            }
            catch (FormatException ex) // Handle errors if Base64 strings are invalid
            {
                Debug.WriteLine($"Error verifying password: Invalid Base64 format. {ex.Message}");
                return false;
            }
            catch (Exception ex) // Catch any other exceptions during verification
            {
                Debug.WriteLine($"Error verifying password: {ex.Message}");
                return false;
            }
        }

        // --- Helper ---
        private RoleUtilisateur ConvertStringToRole(string roleString)
        {
            if (Enum.TryParse<RoleUtilisateur>(roleString, ignoreCase: true, out var role))
            {
                return role;
            }
            Debug.WriteLine($"Warning: Could not parse role string '{roleString}'. Defaulting to SimpleUser.");
            return RoleUtilisateur.SimpleUser; // Default role
        }
    }
}