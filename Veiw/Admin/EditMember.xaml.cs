using System;
using System.Windows;
using System.Windows.Controls;
using System.Linq;
using MySql.Data.MySqlClient; // Keep for MySqlException
using UserModels;
using System.Collections.Generic; // For Dictionary
using DataGridNamespace; // Add for DatabaseConnection
using System.Diagnostics; // For Debug

namespace DataGridNamespace.Admin
{
    public partial class EditMember : Window
    {
        private readonly User _user;
        private readonly Action _refreshCallback; // Action delegate to refresh the calling view

        public EditMember(User user, Action refreshCallback = null)
        {
            InitializeComponent();
            _user = user ?? throw new ArgumentNullException(nameof(user)); // Null check
            _refreshCallback = refreshCallback;

            // Populate fields with user data
            PopulateFields();
        }

        /// <summary>
        /// Populates the UI fields with the current user's data.
        /// </summary>
        private void PopulateFields()
        {
            UserIdTextBox.Text = _user.Id.ToString();
            NameTextBox.Text = _user.Nom;
            EmailTextBox.Text = _user.Email;

            // Set the role in the ComboBox based on the user's Role enum value
            // Use case-insensitive comparison for safety
            var roleItem = RoleComboBox.Items.Cast<ComboBoxItem>()
                .FirstOrDefault(item => item.Content.ToString().Equals(_user.Role.ToString(), StringComparison.OrdinalIgnoreCase));

            if (roleItem != null)
            {
                RoleComboBox.SelectedItem = roleItem;
            }
            else
            {
                // Handle case where role might not be in the ComboBox (e.g., if DB has roles not listed)
                Debug.WriteLine($"Warning: User role '{_user.Role}' not found in RoleComboBox.");
                // Optionally add the role or select a default
                RoleComboBox.SelectedIndex = -1; // No selection
            }
        }

        /// <summary>
        /// Handles the Save button click event. Validates input and updates the user in the database.
        /// </summary>
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Validate input fields
            if (!ValidateInput(out string name, out string email, out string role))
            {
                return; // Validation failed, message shown in ValidateInput
            }

            try
            {
                // Prepare query and parameters using DatabaseConnection
                string query = "UPDATE users SET Nom = @Name, Role = @Role, Email = @Email WHERE Id = @Id";
                var parameters = new Dictionary<string, object>
                {
                    { "@Name", name },
                    { "@Role", role },
                    { "@Email", email },
                    { "@Id", _user.Id }
                };

                int rowsAffected = DatabaseConnection.ExecuteNonQuery(query, parameters);

                if (rowsAffected > 0)
                {
                    // Update the local user object to reflect changes immediately
                    _user.Nom = name;
                    _user.Email = email;
                    // Safely parse the role string back to the enum
                    if (Enum.TryParse<RoleUtilisateur>(role, ignoreCase: true, out var parsedRole))
                    {
                        _user.Role = parsedRole;
                    }
                    else
                    {
                        Debug.WriteLine($"Warning: Could not parse role '{role}' back to enum after update.");
                        // Keep the original role in the object if parsing fails
                    }


                    MessageBox.Show("Member updated successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                    // Invoke the callback to refresh the parent view (e.g., MembersListView)
                    _refreshCallback?.Invoke();

                    this.DialogResult = true; // Indicate success
                    this.Close(); // Close the edit window
                }
                else
                {
                    MessageBox.Show("No changes were saved. The user data might be the same or the user ID is invalid.", "Update Info", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (MySqlException dbEx)
            {
                Debug.WriteLine($"Database error updating member: {dbEx.Message}");
                MessageBox.Show("A database error occurred while updating the member.", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
                // Optionally handle specific DB errors like duplicate email
                if (dbEx.Number == 1062) // Duplicate entry
                {
                    MessageBox.Show("The email address might already be in use by another member.", "Duplicate Email", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error updating member: {ex.Message}");
                MessageBox.Show($"An unexpected error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Validates the input fields before saving.
        /// </summary>
        /// <returns>True if validation passes, false otherwise.</returns>
        private bool ValidateInput(out string name, out string email, out string role)
        {
            name = NameTextBox.Text.Trim();
            email = EmailTextBox.Text.Trim();
            role = (RoleComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();

            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Please enter a username.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                NameTextBox.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(email))
            {
                MessageBox.Show("Please enter an email address.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                EmailTextBox.Focus();
                return false;
            }
            // Basic email format check (consider more robust validation if needed)
            if (!email.Contains('@') || !email.Contains('.'))
            {
                MessageBox.Show("Please enter a valid email address.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                EmailTextBox.Focus();
                return false;
            }


            if (string.IsNullOrWhiteSpace(role))
            {
                MessageBox.Show("Please select a role.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                RoleComboBox.Focus();
                return false;
            }

            return true;
        }


        /// <summary>
        /// Handles the Cancel button click event. Closes the window without saving.
        /// </summary>
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false; // Indicate cancellation
            this.Close();
        }
    }
}