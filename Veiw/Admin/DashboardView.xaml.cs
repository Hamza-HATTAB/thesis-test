using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input; // Needed for MouseButtonEventArgs if handlers were present
using System.Windows.Media;
using System.Windows.Navigation;
using DataGridNamespace.Admin;
using DataGridNamespace; // For Session, DatabaseConnection
using System.Data;
using System.Diagnostics;
using System.Globalization; // For IValueConverter CultureInfo
using System.Windows.Data;   // For IValueConverter

namespace DataGridNamespace
{
    // ===============================================================
    // Local Converters (if not defined globally)
    // ===============================================================
    public class BooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool flag = false;
            if (value is bool b) flag = b;
            else if (value is bool?) flag = ((bool?)value).GetValueOrDefault();

            bool invert = parameter?.ToString().Equals("Invert", StringComparison.OrdinalIgnoreCase) ?? false;
            if (invert) flag = !flag;

            return flag ? Visibility.Visible : Visibility.Collapsed;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
    // ===============================================================
    // END Local Converters
    // ===============================================================


    /// <summary>
    /// Interaction logic for DashboardView.xaml - Hosts the main navigation frame.
    /// </summary>
    public partial class DashboardView : Page
    {
        private bool databaseConnectionValid = false;

        public DashboardView()
        {
            InitializeComponent();
            ValidateDatabaseConnection();
            // Set initial button background state (optional)
            HighlightButton(DashboardButton); // Highlight Dashboard by default
        }

        /// <summary>
        /// Checks database connectivity on startup.
        /// </summary>
        private void ValidateDatabaseConnection()
        {
            try
            {
                // Use the TestConnection method for a quick check
                databaseConnectionValid = DatabaseConnection.TestConnection();
                UpdateConnectionStatusIndicator(); // Update visual indicator if any

                if (!databaseConnectionValid)
                {
                    ShowMessage("Database connection could not be established. Some features may be unavailable.", "DB Warning", MessageBoxImage.Warning);
                }
            }
            catch (Exception ex) // Catch potential errors during test
            {
                databaseConnectionValid = false;
                UpdateConnectionStatusIndicator();
                ShowMessage($"Error testing database connection: {ex.Message}", "DB Error", MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Updates visual indicators of database connection status (Placeholder).
        /// </summary>
        private void UpdateConnectionStatusIndicator()
        {
            // Example: StatusIndicatorDot.Fill = databaseConnectionValid ? Brushes.LimeGreen : Brushes.OrangeRed;
            Debug.WriteLine($"Dashboard: Database connection status valid: {databaseConnectionValid}");
        }

        /// <summary>
        /// Helper to visually highlight the selected navigation button.
        /// </summary>
        private void HighlightButton(Button selectedButton)
        {
            // Define the highlight color
            SolidColorBrush highlightBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#7B5CD6")); // Purple highlight
            SolidColorBrush transparentBrush = Brushes.Transparent; // Default background

            // Reset all buttons
            DashboardButton.Background = transparentBrush;
            ThesisButton.Background = transparentBrush;
            MembersButton.Background = transparentBrush;
            ProfileButton.Background = transparentBrush;
            FavoritesButton.Background = transparentBrush;

            // Apply highlight to the selected button
            if (selectedButton != null)
            {
                selectedButton.Background = highlightBrush;
            }
        }

        // --- Navigation Button Event Handlers ---

        private void DashboardButton_Click(object sender, RoutedEventArgs e)
        {
            HighlightButton(DashboardButton);
            MainFrame.Content = null; // Clear content or load dashboard summary widget page
            Debug.WriteLine("Dashboard Button Clicked.");
        }

        private void ThesisButton_Click(object sender, RoutedEventArgs e)
        {
            NavigateMainFrame(typeof(ThesisView), ThesisButton);
        }

        private void MembersButton_Click(object sender, RoutedEventArgs e)
        {
            NavigateMainFrame(typeof(MembersListView), MembersButton);
        }

        private void ProfileButton_Click(object sender, RoutedEventArgs e)
        {
            NavigateMainFrame(typeof(ProfileView), ProfileButton); // Assuming ProfileView exists
        }

        // Add this method to handle favorites button click
        private void FavoritesButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Navigate to favorites view
                // You'll need to implement the actual navigation logic based on your app structure
                HighlightButton(FavoritesButton);
                // Example: MainFrame.Navigate(new FavoritesView());
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Navigation to Favorites failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Helper method to navigate the MainFrame and highlight the button.
        /// </summary>
        private void NavigateMainFrame(Type pageType, Button buttonToHighlight)
        {
            if (MainFrame == null) { ShowMessage("Navigation frame is missing.", "UI Error", MessageBoxImage.Error); return; }

            try
            {
                // Avoid re-navigating to the same page type
                if (MainFrame.Content?.GetType() == pageType) return;

                HighlightButton(buttonToHighlight); // Highlight the button first
                MainFrame.Content = Activator.CreateInstance(pageType); // Set content directly
                Debug.WriteLine($"Navigated MainFrame to {pageType.Name}");
            }
            catch (Exception ex)
            {
                ShowMessage($"Error navigating to {pageType.Name}: {ex.Message}", "Navigation Error", MessageBoxImage.Error);
                Debug.WriteLine($"Navigation Error ({pageType.Name}): {ex}");
            }
        }

        // --- Logout and Window Controls ---

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            // Delegate logout to the parent MainWindow
            var parentWindow = Window.GetWindow(this) as MainWindow;
            parentWindow?.LogoutButton_Click(sender, e); // Call MainWindow's logout logic
        }

        // Window control button clicks should ideally be handled by the parent MainWindow,
        // as the Page itself doesn't control the window state.
        // These methods find the parent window and ask it to perform the action.
        /// <summary>
        /// Minimizes the parent window
        /// </summary>
        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Window parentWindow = Window.GetWindow(this);
                if (parentWindow != null)
                {
                    parentWindow.WindowState = WindowState.Minimized;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Minimize failed: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Toggles between maximized and normal window state
        /// </summary>
        private void MaximizeRestoreButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Window parentWindow = Window.GetWindow(this);
                if (parentWindow != null)
                {
                    if (parentWindow.WindowState == WindowState.Maximized)
                    {
                        parentWindow.WindowState = WindowState.Normal;
                    }
                    else
                    {
                        parentWindow.WindowState = WindowState.Maximized;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Maximize/Restore failed: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Closes the parent window
        /// </summary>
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Window parentWindow = Window.GetWindow(this);
                parentWindow?.Close();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Close failed: {ex.Message}");
            }
        }

        // --- Helper for Messages ---
        private void ShowMessage(string message, string title, MessageBoxImage icon)
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, icon);
        }

        /// <summary>
        /// Handles mouse down events on the border for window dragging
        /// </summary>
        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (e.LeftButton == MouseButtonState.Pressed)
                {
                    // For Page controls, we need to find the parent window
                    Window parentWindow = Window.GetWindow(this);
                    if (parentWindow != null)
                    {
                        parentWindow.DragMove();
                    }
                }
            }
            catch (InvalidOperationException ex)
            {
                Debug.WriteLine($"DragMove failed: {ex.Message}");
                // This can happen if the window is in a state that doesn't allow dragging
            }
        }

        /// <summary>
        /// Handles double-click events on the border to maximize/restore window
        /// </summary>
        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (e.ClickCount == 2)
                {
                    Window parentWindow = Window.GetWindow(this);
                    if (parentWindow != null)
                    {
                        if (parentWindow.WindowState == WindowState.Maximized)
                        {
                            parentWindow.WindowState = WindowState.Normal;
                        }
                        else
                        {
                            parentWindow.WindowState = WindowState.Maximized;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Window state change failed: {ex.Message}");
            }
        }

    } // End of DashboardView Class
} // End of namespace