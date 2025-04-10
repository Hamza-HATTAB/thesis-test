using System;
using System.Windows;
using System.Windows.Controls;
using DataGridNamespace; // Ensure this namespace is included
// Removed using DataGridNamespace.Models; - Session is not in this namespace

namespace DataGridNamespace.Admin
{
    public partial class AdminSidebar : UserControl
    {
        public AdminSidebar()
        {
            try
            {
                InitializeComponent();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing AdminSidebar: {ex.Message}\nStack Trace: {ex.StackTrace}",
                              "Initialization Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DashboardButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var mainWindow = Window.GetWindow(this) as MainWindow;
                mainWindow?.NavigateToDashboard(); // Use null-conditional operator
                if (mainWindow == null)
                {
                    MessageBox.Show("Could not find the main window for navigation.",
                                  "Navigation Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error in DashboardButton_Click: {ex.Message}\nStack Trace: {ex.StackTrace}",
                              "Navigation Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ThesisButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var mainWindow = Window.GetWindow(this) as MainWindow;
                mainWindow?.NavigateToThesis(); // Use null-conditional operator
                if (mainWindow == null)
                {
                    MessageBox.Show("Could not find the main window for navigation.",
                                  "Navigation Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error in ThesisButton_Click: {ex.Message}\nStack Trace: {ex.StackTrace}",
                              "Navigation Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void FavoritesButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var mainWindow = Window.GetWindow(this) as MainWindow;
                mainWindow?.NavigateToFavorites(); // Use null-conditional operator
                if (mainWindow == null)
                {
                    MessageBox.Show("Could not find the main window for navigation.",
                                  "Navigation Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error in FavoritesButton_Click: {ex.Message}\nStack Trace: {ex.StackTrace}",
                              "Navigation Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MembersManagementButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var mainWindow = Window.GetWindow(this) as MainWindow;
                mainWindow?.NavigateToMembersManagement(); // Use null-conditional operator
                if (mainWindow == null)
                {
                    MessageBox.Show("Could not find the main window for navigation.",
                                  "Navigation Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error in MembersManagementButton_Click: {ex.Message}\nStack Trace: {ex.StackTrace}",
                              "Navigation Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ProfileButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var mainWindow = Window.GetWindow(this) as MainWindow;
                mainWindow?.NavigateToProfile(); // Use null-conditional operator
                if (mainWindow == null)
                {
                    MessageBox.Show("Could not find the main window for navigation.",
                                  "Navigation Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error in ProfileButton_Click: {ex.Message}\nStack Trace: {ex.StackTrace}",
                              "Navigation Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var mainWindow = Window.GetWindow(this) as MainWindow;
                if (mainWindow != null)
                {
                    // Clear the session before logging out by calling the main window's logout
                    // Session.ClearSession(); // Build Error: No ClearSession method. Session.Logout() handles this internally.
                    mainWindow.LogoutButton_Click(sender, e); // Trigger logout from MainWindow which handles Session.Logout()
                }
                else
                {
                    MessageBox.Show("Could not find the main window for logout.",
                                  "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error in LogoutButton_Click: {ex.Message}\nStack Trace: {ex.StackTrace}",
                              "Logout Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}