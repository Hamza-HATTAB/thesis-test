using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace DataGridNamespace
{
    public partial class Login : Window
    {
        public Login()
        {
            InitializeComponent();
        }

        private void CloseImage_MouseUp(object sender, MouseButtonEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                this.DragMove();
        }

        private void GoToLayout2_Click(object sender, RoutedEventArgs e)
        {
            Layout1.Visibility = Visibility.Collapsed;
            Layout2.Visibility = Visibility.Visible;
        }

        private void GoToLayout1_Click(object sender, RoutedEventArgs e)
        {
            Layout2.Visibility = Visibility.Collapsed;
            Layout1.Visibility = Visibility.Visible;
        }

        // Layout1 Events
        private void textUser_L1_MouseDown(object sender, MouseButtonEventArgs e)
        {
            txtUser_L1.Focus();
        }

        private void txtUser_L1_TextChanged(object sender, TextChangedEventArgs e)
        {
            textUser_L1.Visibility = string.IsNullOrEmpty(txtUser_L1.Text)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private void textPassword_L1_MouseDown(object sender, MouseButtonEventArgs e)
        {
            txtPassword_L1.Focus();
        }

        private void txtPassword_L1_PasswordChanged(object sender, RoutedEventArgs e)
        {
            textPassword_L1.Visibility = string.IsNullOrEmpty(txtPassword_L1.Password)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        // زر تسجيل الدخول في Layout1
        private void SignIn_L1_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(txtUser_L1.Text) || string.IsNullOrEmpty(txtPassword_L1.Password))
            {
                MessageBox.Show("Please fill Username & Password.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // Use our enhanced secure login method with the centralized database connection
                var loginParams = new Dictionary<string, object>
                {
                    ["@Nom"] = txtUser_L1.Text,
                    ["@Password"] = txtPassword_L1.Password // In a production app, use password hashing
                };

                // Use DatabaseConnection to execute the query
                string loginQuery = "SELECT id, nom, email, role FROM users WHERE Nom=@Nom AND Password=@Password";
                using (var reader = DatabaseConnection.ExecuteReader(loginQuery, loginParams))
                {
                    if (reader.Read())
                    {
                        // Successfully found the user
                        int userId = reader.GetInt32("id");
                        string userName = reader.GetString("nom");
                        string userEmail = reader.GetString("email");
                        string role = reader.GetString("role");
                        
                        // Initialize session with user information
                      Session.InitializeSession(userId, role, userName);
                        
                        // Log the successful login attempt
                        LogLoginAttempt(userId, true);
                        
                        MessageBox.Show($"Welcome {userName}! Login successful.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                        
                        // Open the main window with appropriate user role
                        var mainWindow = new MainWindow(role, userId);
                        mainWindow.Show();
                        this.Close();
                    }
                    else
                    {
                        // Log the failed login attempt
                        LogLoginAttempt(0, false, txtUser_L1.Text);
                        
                        MessageBox.Show("Invalid username or password.", "Login Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the exception using Debug for better diagnostic capabilities
                System.Diagnostics.Debug.WriteLine($"Login error: {ex.Message}\n{ex.StackTrace}");
                
                MessageBox.Show("An error occurred during login. Please try again later.", 
                    "Login Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        /// <summary>
        /// Logs login attempts for security auditing
        /// </summary>
        private void LogLoginAttempt(int userId, bool successful, string attemptedUsername = null)
        {
            try
            {
                var logParams = new Dictionary<string, object>
                {
                    ["@UserId"] = userId,
                    ["@Successful"] = successful ? 1 : 0,
                    ["@AttemptedUsername"] = successful ? DBNull.Value : (object)attemptedUsername,
                    ["@IpAddress"] = "127.0.0.1", // In a real app, get the actual IP
                    ["@Timestamp"] = DateTime.Now
                };

                // Check if the login_logs table exists, if not create it
                string createTableQuery = @"CREATE TABLE IF NOT EXISTS login_logs (
                    id INT AUTO_INCREMENT PRIMARY KEY,
                    user_id INT,
                    successful TINYINT(1),
                    attempted_username VARCHAR(255),
                    ip_address VARCHAR(50),
                    timestamp DATETIME
                )";
                
                DatabaseConnection.ExecuteNonQuery(createTableQuery);

                // Log the login attempt
                string logQuery = @"INSERT INTO login_logs 
                    (user_id, successful, attempted_username, ip_address, timestamp) 
                    VALUES (@UserId, @Successful, @AttemptedUsername, @IpAddress, @Timestamp)";
                
                DatabaseConnection.ExecuteNonQuery(logQuery, logParams);
            }
            catch (Exception ex)
            {
                // Just log using Debug, don't disrupt the user experience
                System.Diagnostics.Debug.WriteLine($"Error logging login attempt: {ex.Message}");
            }
        }

        // Layout2 Events (للتسجيل)
        private void textUser_L2_MouseDown(object sender, MouseButtonEventArgs e)
        {
            txtUser_L2.Focus();
        }

        private void txtUser_L2_TextChanged(object sender, TextChangedEventArgs e)
        {
            textUser_L2.Visibility = string.IsNullOrEmpty(txtUser_L2.Text)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private void textEmail_L2_MouseDown(object sender, MouseButtonEventArgs e)
        {
            txtEmail_L2.Focus();
        }

        private void txtEmail_L2_TextChanged(object sender, TextChangedEventArgs e)
        {
            textEmail_L2.Visibility = string.IsNullOrEmpty(txtEmail_L2.Text)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private void textPassword_L2_MouseDown(object sender, MouseButtonEventArgs e)
        {
            txtPassword_L2.Focus();
        }

        private void txtPassword_L2_PasswordChanged(object sender, RoutedEventArgs e)
        {
            textPassword_L2.Visibility = string.IsNullOrEmpty(txtPassword_L2.Password)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private void SignUp_L2_Click(object sender, RoutedEventArgs e)
        {
            // Enhanced validation
            if (string.IsNullOrEmpty(txtUser_L2.Text) || string.IsNullOrEmpty(txtEmail_L2.Text) || string.IsNullOrEmpty(txtPassword_L2.Password))
            {
                MessageBox.Show("Please fill Username, Email & Password.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Validate email format
            if (!IsValidEmail(txtEmail_L2.Text))
            {
                MessageBox.Show("Invalid Email!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Enhanced password validation
            if (txtPassword_L2.Password.Length < 6)
            {
                MessageBox.Show("Password must be at least 6 characters long.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Get selected role from the combo box
            var selectedItem = roleCombo_L2.SelectedItem as ComboBoxItem;
            string roleValue = (selectedItem != null) ? selectedItem.Content.ToString() : "simpleuser";

            try
            {
                // Check if email is already used
                var checkParams = new Dictionary<string, object> { ["@Email"] = txtEmail_L2.Text };
                string checkQuery = "SELECT COUNT(*) FROM users WHERE Email = @Email";
                int count = Convert.ToInt32(DatabaseConnection.ExecuteScalar(checkQuery, checkParams));
                
                if (count > 0)
                {
                    MessageBox.Show("Cet email est déjà utilisé.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                
                // Also check if username is already taken
                var checkUserParams = new Dictionary<string, object> { ["@Nom"] = txtUser_L2.Text };
                string checkUserQuery = "SELECT COUNT(*) FROM users WHERE Nom = @Nom";
                int userCount = Convert.ToInt32(DatabaseConnection.ExecuteScalar(checkUserQuery, checkUserParams));
                
                if (userCount > 0)
                {
                    MessageBox.Show("This username is already taken.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Insert the new user with enhanced parameters
                var insertParams = new Dictionary<string, object>
                {
                    ["@Nom"] = txtUser_L2.Text,
                    ["@Email"] = txtEmail_L2.Text,
                    ["@Password"] = txtPassword_L2.Password, // In production, hash this password
                    ["@Role"] = roleValue,
                    ["@CreatedAt"] = DateTime.Now
                };
                
                string insertQuery = "INSERT INTO users (Nom, Email, Password, Role, created_at) VALUES (@Nom, @Email, @Password, @Role, @CreatedAt)";
                
                int rowsAffected = DatabaseConnection.ExecuteNonQuery(insertQuery, insertParams);
                
                if (rowsAffected > 0)
                {
                    // Log the successful registration
                    LogRegistrationActivity(txtUser_L2.Text, txtEmail_L2.Text, roleValue, true);
                    
                    MessageBox.Show($"Inscription réussie en tant que {roleValue} !", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    
                    // Return to login screen
                    Layout2.Visibility = Visibility.Collapsed;
                    Layout1.Visibility = Visibility.Visible;
                }
                else
                {
                    LogRegistrationActivity(txtUser_L2.Text, txtEmail_L2.Text, roleValue, false, "No rows affected");
                    MessageBox.Show("Registration failed. Please try again.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                // Log the error
                LogRegistrationActivity(txtUser_L2.Text, txtEmail_L2.Text, roleValue, false, ex.Message);
                System.Diagnostics.Debug.WriteLine($"Registration error: {ex.Message}\n{ex.StackTrace}");
                
                // Show user-friendly error message
                MessageBox.Show("An error occurred during registration. Please try again later.", 
                    "Registration Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        /// <summary>
        /// Logs registration activity for auditing and security purposes
        /// </summary>
        private void LogRegistrationActivity(string username, string email, string role, bool successful, string errorMessage = null)
        {
            try
            {
                // Create the registration_logs table if it doesn't exist
                string createTableQuery = @"CREATE TABLE IF NOT EXISTS registration_logs (
                    id INT AUTO_INCREMENT PRIMARY KEY,
                    username VARCHAR(255),
                    email VARCHAR(255),
                    role VARCHAR(50),
                    successful TINYINT(1),
                    error_message TEXT,
                    ip_address VARCHAR(50),
                    timestamp DATETIME
                )";
                
                DatabaseConnection.ExecuteNonQuery(createTableQuery);
                
                // Log the registration attempt
                var logParams = new Dictionary<string, object>
                {
                    ["@Username"] = username,
                    ["@Email"] = email,
                    ["@Role"] = role,
                    ["@Successful"] = successful ? 1 : 0,
                    ["@ErrorMessage"] = errorMessage != null ? (object)errorMessage : DBNull.Value,
                    ["@IpAddress"] = "127.0.0.1", // In a real app, get the actual IP
                    ["@Timestamp"] = DateTime.Now
                };
                
                string logQuery = @"INSERT INTO registration_logs 
                    (username, email, role, successful, error_message, ip_address, timestamp) 
                    VALUES (@Username, @Email, @Role, @Successful, @ErrorMessage, @IpAddress, @Timestamp)";
                
                DatabaseConnection.ExecuteNonQuery(logQuery, logParams);
            }
            catch (Exception ex)
            {
                // Just log with Debug, don't disrupt the user experience
                System.Diagnostics.Debug.WriteLine($"Error logging registration activity: {ex.Message}");
            }
        }

        private bool IsValidEmail(string email)
        {
            var regex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
            return regex.IsMatch(email);
        }
    }
}
