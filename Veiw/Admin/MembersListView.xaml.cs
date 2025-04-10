// ===============================================================
// USING DIRECTIVES
// ===============================================================
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using MySql.Data.MySqlClient;
using UserModels; // CRITICAL: Ensure this namespace contains User and RoleUtilisateur enum
using System.Diagnostics;
using System.Globalization;
using DataGridNamespace; // For DatabaseConnection and EditMember

namespace DataGridNamespace.Admin // START NAMESPACE - Ensure matches XAML xmlns:local
{
    // ===============================================================
    // Converters - Defined ONCE, directly inside the namespace block
    // ===============================================================
    public class RowNumberConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DataGridRow row)
            {
                var dataGrid = ItemsControl.ItemsControlFromItemContainer(row) as DataGrid;
                if (dataGrid?.ItemsSource != null)
                {
                    int index = dataGrid.Items.IndexOf(row.DataContext);
                    if (index >= 0) return index + 1;
                }
                try { return row.GetIndex() + 1; } catch { return null; } // Fallback
            }
            return null;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class StringToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isEmpty = string.IsNullOrEmpty(value as string);
            bool invert = parameter?.ToString().Equals("Inverted", StringComparison.OrdinalIgnoreCase) ?? false;
            // Visible if (empty AND not inverted) OR (not empty AND inverted)
            return (isEmpty ^ invert) ? Visibility.Visible : Visibility.Collapsed;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class RoleToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is RoleUtilisateur role)
            {
                return role switch
                {
                    RoleUtilisateur.Admin => Brushes.Indigo,
                    RoleUtilisateur.Etudiant => Brushes.DodgerBlue,
                    RoleUtilisateur.SimpleUser => Brushes.SeaGreen,
                    _ => Brushes.SlateGray
                };
            }
            return Brushes.SlateGray;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class NameToInitialsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string name && !string.IsNullOrWhiteSpace(name))
            {
                var parts = name.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length > 0)
                {
                    if (parts.Length == 1)
                        return parts[0].Length > 1 ? parts[0].Substring(0, 2).ToUpper() : parts[0].Substring(0, 1).ToUpper();
                    return (parts[0][0].ToString() + parts[^1][0].ToString()).ToUpper();
                }
            }
            return "?";
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
    // ===============================================================
    // END Converters
    // ===============================================================


    // ===============================================================
    // MembersListView Class - Defined ONCE
    // ===============================================================
    public partial class MembersListView : Page
    {
        // --- Fields --- Defined ONCE
        private List<User> allMembers;
        private CollectionViewSource membersViewSource;

        // --- Constructor --- Defined ONCE
        public MembersListView()
        {
            InitializeComponent();
            LoadMembers();
        }

        // --- Methods --- Defined ONCE

        private void LoadMembers()
        {
            try
            {
                allMembers = new List<User>();
                string query = "SELECT Id, Nom, Email, Role FROM users ORDER BY Nom ASC";

                using (var reader = DatabaseConnection.ExecuteReader(query))
                {
                    if (reader == null) { ShowError("DB connection failed for members."); return; }
                    while (reader.Read())
                    {
                        allMembers.Add(new User
                        {
                            Id = reader.GetInt32("Id"),
                            Nom = reader.GetString("Nom"),
                            Email = reader.GetString("Email"),
                            Role = ConvertStringToRole(reader.GetString("Role"))
                        });
                    }
                }

                membersViewSource = new CollectionViewSource { Source = allMembers };
                membersViewSource.Filter += MembersViewSource_Filter;
                MembersDataGrid.ItemsSource = membersViewSource.View;

                Debug.WriteLine($"Loaded {allMembers.Count} members.");
            }
            catch (MySqlException dbEx) { ShowError("DB error loading members.", dbEx); }
            catch (Exception ex) { ShowError("Unexpected error loading members.", ex); }
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            membersViewSource?.View?.Refresh();
        }

        private void MembersViewSource_Filter(object sender, FilterEventArgs e)
        {
            if (SearchTextBox == null || string.IsNullOrWhiteSpace(SearchTextBox.Text))
            { e.Accepted = true; return; }

            if (e.Item is User user)
            {
                string searchText = SearchTextBox.Text.ToLowerInvariant().Trim();
                e.Accepted = user.Nom.ToLowerInvariant().Contains(searchText) ||
                             user.Email.ToLowerInvariant().Contains(searchText) ||
                             user.Role.ToString().ToLowerInvariant().Contains(searchText) ||
                             user.Id.ToString().Contains(searchText);
            }
            else { e.Accepted = false; }
        }

        private RoleUtilisateur ConvertStringToRole(string roleString)
        {
            if (Enum.TryParse<RoleUtilisateur>(roleString, ignoreCase: true, out var role))
            { return role; }
            Debug.WriteLine($"Warning: Unknown role '{roleString}'. Defaulting.");
            return RoleUtilisateur.SimpleUser;
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button { Tag: User userToEdit })
            {
                // Ensure EditMember constructor matches this call exactly
                var editWindow = new EditMember(userToEdit, LoadMembers);
                editWindow.Owner = Window.GetWindow(this);
                editWindow.ShowDialog();
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button { Tag: User userToDelete })
            {
                var result = MessageBox.Show($"Delete '{userToDelete.Nom}'?", "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        string query = "DELETE FROM users WHERE Id = @Id";
                        var parameters = new Dictionary<string, object> { { "@Id", userToDelete.Id } };
                        int rowsAffected = DatabaseConnection.ExecuteNonQuery(query, parameters);
                        if (rowsAffected > 0)
                        {
                            MessageBox.Show("User deleted.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                            LoadMembers(); // Refresh grid
                        }
                        else { MessageBox.Show("User not found.", "Deletion Failed", MessageBoxButton.OK, MessageBoxImage.Warning); }
                    }
                    catch (MySqlException dbEx) when (dbEx.Number == 1451) { ShowError($"Cannot delete '{userToDelete.Nom}'. User has related records.", dbEx); }
                    catch (Exception ex) { ShowError("Error deleting user.", ex); }
                }
            }
        }

        private void ShowError(string message, Exception ex = null)
        {
            string fullMessage = message + (ex != null ? $"\nDetails: {ex.Message}" : "");
            Debug.WriteLine($"ERROR in MembersListView: {fullMessage}" + (ex != null ? $"\n{ex.StackTrace}" : ""));
            MessageBox.Show(fullMessage, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    // ===============================================================
    // END MembersListView Class
    // ===============================================================

} // END NAMESPACE