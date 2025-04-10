using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Navigation;
using DataGridNamespace.Admin; // Namespace for Admin views
using DataGridNamespace;       // Namespace for Session, DBConnection, Login, ProfileView etc.
// Removed: using DataGridNamespace.Models; // Session is not here
using System.Diagnostics;
using System.Data; // For ConnectionState
// Add this namespace for the icon package
using MahApps.Metro.IconPacks;

// Make sure there isn't a stray character or incomplete statement before this line
namespace DataGridNamespace
{
    /// <summary>
    /// Main application window. Hosts the sidebar and main content frame.
    /// Handles top-level navigation and window controls.
    /// </summary>
    public partial class MainWindow : Window // Ensure this matches XAML: x:Class="DataGridNamespace.MainWindow"
    {
        // Fields defined ONCE
        private string currentUserRole;
        private int currentUserId;
        // Remove the unused field to fix the warning
        // private bool isMaximized = false;

        /// <summary>
        /// Constructor defined ONCE
        /// </summary>
        public MainWindow(string userRole, int userId)
        {
            InitializeComponent(); // This method comes from the WPF framework (partial class)
            try
            {
                this.currentUserRole = userRole;
                this.currentUserId = userId;

                InitializeOrVerifySession(userId, userRole);
                SetupUserInterface();

                // Use Loaded event for safer initial navigation
                this.Loaded += MainWindow_Loaded;
            }
            catch (Exception ex)
            {
                LogAndShowFatalError("MainWindow Initialization Error", ex);
                // Consider Application.Current.Shutdown();
            }
        }

        /// <summary>
        /// Handles the Loaded event to perform initial navigation safely.
        /// </summary>
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                NavigateToInitialView();
            }
            catch (Exception ex)
            {
                LogAndShowNavigationError("Error during initial navigation", ex);
            }
        }


        /// <summary>
        /// Ensures the Session static class reflects the current user.
        /// </summary>
        private void InitializeOrVerifySession(int userId, string userRole)
        {
            // Check if the session needs initialization or just verification/refresh
            if (!Session.IsLoggedIn || Session.CurrentUserId != userId || Session.CurrentUserRole != userRole)
            {
                string userName = GetUserNameFromDatabase(userId);
                // InitializeSession handles setting all Session properties and logging
                Session.InitializeSession(userId, userRole, userName);
            }
            else
            {
                // Session is likely valid, just refresh the activity timestamp
                Session.RefreshSession();
                Debug.WriteLine($"Session verified and refreshed for User ID: {userId}");
            }
        }

        /// <summary>
        /// Retrieves the username from the database. Returns "User" on failure.
        /// </summary>
        private string GetUserNameFromDatabase(int userId)
        {
            const string defaultUserName = "User";
            try
            {
                string query = "SELECT nom FROM users WHERE id = @UserId";
                var parameters = new Dictionary<string, object> { { "@UserId", userId } };
                // ExecuteScalar returns the first column of the first row, or null
                var result = DatabaseConnection.ExecuteScalar(query, parameters);

                if (result != null && result != DBNull.Value)
                {
                    return result.ToString();
                }
                Debug.WriteLine($"Username not found for User ID: {userId}. Using default '{defaultUserName}'.");
                return defaultUserName;
            }
            catch (Exception ex)
            {
                // Log the error but don't crash the UI, return default name
                Debug.WriteLine($"Database error retrieving username for User ID {userId}: {ex.Message}");
                return defaultUserName;
            }
        }

        /// <summary>
        /// Sets up the sidebar based on user role and initial content.
        /// </summary>
        private void SetupUserInterface()
        {
            try
            {
                if (SidebarContainer == null)
                {
                    // If SidebarContainer is null, the XAML likely didn't load correctly or x:Name is missing/wrong.
                    LogAndShowFatalError("Critical Error: SidebarContainer control not found in MainWindow.xaml.", null);
                    return;
                }

                // Load the appropriate sidebar based on user role
                if (currentUserRole.Equals("admin", StringComparison.OrdinalIgnoreCase))
                {
                    // Load the AdminSidebar for admin users
                    var adminSidebar = new AdminSidebar();
                    SidebarContainer.Content = adminSidebar;
                    adminSidebar.Background = new SolidColorBrush(Color.FromRgb(44, 62, 80)); // #2C3E50
                }
                else
                {
                    // For non-admin users, load a regular sidebar with limited functionality
                    // We'll use a green-themed AdminSidebar but hide the admin-specific buttons
                    var userSidebar = new AdminSidebar(); // Reusing AdminSidebar but with different styling
                    
                    // Hide admin-specific buttons
                    if (userSidebar.FindName("MembersManagementButton") is Button membersBtn)
                        membersBtn.Visibility = Visibility.Collapsed;
                    
                    SidebarContainer.Content = userSidebar;
                    userSidebar.Background = new SolidColorBrush(Color.FromRgb(0, 128, 0)); // Green color
                }
            }
            catch (Exception ex)
            {
                LogAndShowFatalError("Error setting up UI elements.", ex);
            }
        }

        /// <summary>
        /// Navigates to the default view (Dashboard) on startup.
        /// </summary>
        private void NavigateToInitialView()
        {
            NavigateToDashboard(); // Default initial view
        }


        // --- Navigation Methods ---

        /// <summary>
        /// Navigates the MainFrame to a specific Page or UserControl type using direct content setting.
        /// </summary>
        /// <param name="pageType">The Type of the Page or UserControl (e.g., typeof(DashboardView)).</param>
        private void NavigateTo(Type pageType)
        {
            if (MainFrame == null)
            {
                LogAndShowError("Navigation Error", "MainFrame control not found."); return;
            }

            try
            {
                // Avoid reloading if already on the target page type
                if (MainFrame.Content?.GetType() == pageType)
                {
                    Debug.WriteLine($"Navigation skipped: Already on page {pageType.Name}.");
                    return;
                }

                // Create an instance of the requested page
                object pageInstanceObj = Activator.CreateInstance(pageType);

                // Check if it's a Page or UserControl and handle accordingly
                if (pageInstanceObj is FrameworkElement element)
                {
                    // Setting Content directly is often more reliable than Frame.Navigate for UserControls/Pages
                    MainFrame.Content = element;
                    // Clear journaling history if setting content directly
                    if (MainFrame.CanGoBack) MainFrame.RemoveBackEntry();
                    if (MainFrame.CanGoForward) { /* Logic to clear forward history if needed */ }

                    Debug.WriteLine($"Successfully navigated to: {pageType.Name}");
                }
                else
                {
                    throw new InvalidOperationException($"The type {pageType.FullName} is not a valid Page or UserControl or could not be instantiated.");
                }
            }
            catch (Exception ex)
            {
                LogAndShowNavigationError($"Error navigating to page: {pageType.Name}", ex);
            }
        }

        // Public navigation methods for sidebar/buttons to call
        public void NavigateToDashboard() => NavigateTo(typeof(DashboardView));
        public void NavigateToMembersManagement() => NavigateTo(typeof(Admin.MembersListView));
        public void NavigateToThesis() => NavigateTo(typeof(Admin.ThesisView));
        public void NavigateToFavorites() => NavigateTo(typeof(Admin.FavoritesView));
        public void NavigateToProfile() => NavigateTo(typeof(ProfileView)); // Ensure ProfileView exists


        // --- Window Chrome Event Handlers ---

        /// <summary>
        /// Allows dragging the window when the left mouse button is pressed down on the title bar area.
        /// </summary>
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Check if the click is on the title bar (approximated by Y coordinate)
            if (e.ButtonState == MouseButtonState.Pressed && e.GetPosition(this).Y < 30) // Assuming title bar height is 30px
            {
                try
                {
                    DragMove();
                }
                catch (InvalidOperationException)
                {
                    // This can happen if the window state is unusual, ignore it.
                }
            }
        }

        // Removed Window_MouseDown as Window_MouseLeftButtonDown covers dragging.

        // Removed Border_MouseLeftButtonDown (double-click maximize handled by button)


        /// <summary>
        /// Handles the Logout button click, confirms with the user, and returns to the Login window.
        /// </summary>
        public void LogoutButton_Click(object sender, RoutedEventArgs e) // Keep public if called externally
        {
            var confirmationWindow = new LogoutConfirmationWindow
            {
                Owner = this // Set owner for proper modal behavior relative to this window
            };

            // ShowDialog returns bool? - true if OK/Yes, false if Cancel/No, null if closed otherwise
            if (confirmationWindow.ShowDialog() == true && confirmationWindow.IsConfirmed)
            {
                try
                {
                    Session.Logout(); // Correctly call Logout to clear session data and log activity
                    Debug.WriteLine("User initiated logout successful.");

                    var loginWindow = new Login();
                    loginWindow.Show(); // Show the login screen

                    this.Close(); // Close this MainWindow
                }
                catch (Exception ex)
                {
                    LogAndShowError("Logout Error", "An error occurred during the logout process.", ex);
                }
            }
            else
            {
                Debug.WriteLine("Logout cancelled by user.");
            }
        }

        /// <summary>
        /// Minimizes the window.
        /// </summary>
        private void MinimizeButton_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

        /// <summary>
        /// Toggles the window between Maximized and Normal states.
        /// </summary>
        private void MaximizeRestoreButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (WindowState == WindowState.Maximized)
                {
                    WindowState = WindowState.Normal;
                    // Update button icon
                    if (MaximizeButton.Content is PackIconMaterial icon)
                    {
                        icon.Kind = PackIconMaterialKind.WindowMaximize;
                    }
                }
                else
                {
                    WindowState = WindowState.Maximized;
                    // Update button icon
                    if (MaximizeButton.Content is PackIconMaterial icon)
                    {
                        icon.Kind = PackIconMaterialKind.WindowRestore;
                    }
                }
            }
            catch (Exception ex)
            {
                LogAndShowFatalError("Error toggling window state", ex);
            }
        }

        /// <summary>
        /// Closes the window and potentially shuts down the application if it's the main window.
        /// </summary>
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            // Optional: Add confirmation before closing if needed
            // MessageBoxResult confirm = MessageBox.Show("Are you sure you want to exit?", "Confirm Exit", MessageBoxButton.YesNo, MessageBoxImage.Question);
            // if (confirm == MessageBoxResult.Yes)
            // {
            this.Close(); // Closes this window
                          // }
        }


        // --- Error Handling Helpers ---
        private void LogAndShowError(string title, string message, Exception ex = null)
        {
            string detail = ex != null ? $"\n\nDetails: {ex.Message}" : "";
            Debug.WriteLine($"ERROR: {title} - {message}{detail}" + (ex != null ? $"\n{ex.StackTrace}" : ""));
            // Ensure MessageBox is shown on the UI thread if called from background
            Dispatcher.Invoke(() => {
                MessageBox.Show(message + detail, title, MessageBoxButton.OK, MessageBoxImage.Error);
            });
        }
        private void LogAndShowNavigationError(string message, Exception ex = null) => LogAndShowError("Navigation Error", message, ex);
        private void LogAndShowFatalError(string message, Exception ex = null) => LogAndShowError("Fatal Error", message, ex);

    } // End of MainWindow Class
} // End of namespace