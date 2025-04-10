using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Diagnostics;

namespace DataGridNamespace.Utilities
{
    /// <summary>
    /// Utility class for platform-specific operations and feature detection
    /// </summary>
    public static class PlatformUtility
    {
        /// <summary>
        /// Indicates whether the current platform is Windows
        /// </summary>
        public static bool IsWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        /// <summary>
        /// Updates a button's appearance to represent a favorite/unfavorite state in a platform-agnostic way
        /// </summary>
        /// <param name="button">The button to update</param>
        /// <param name="isFavorite">Whether the item is a favorite</param>
        public static void UpdateFavoriteButtonAppearance(Button button, bool isFavorite)
        {
            if (button == null) return;

            // Use platform-independent properties for visual feedback
            button.Background = new SolidColorBrush(isFavorite ? 
                Color.FromRgb(255, 200, 200) : Color.FromRgb(240, 240, 240));
            
            button.BorderBrush = new SolidColorBrush(isFavorite ? Colors.Red : Colors.Gray);
            
            // Add a tooltip for accessibility
            button.ToolTip = isFavorite ? "Remove from favorites" : "Add to favorites";
            
            // Store state in Tag for later retrieval
            button.Tag = isFavorite;
        }
        
        /// <summary>
        /// Creates a simple heart shape icon using standard WPF controls
        /// that works across all platforms
        /// </summary>
        public static UIElement CreateHeartIcon(bool filled)
        {
            // Create a viewbox to contain our icon
            var viewbox = new Viewbox
            {
                Width = 16,
                Height = 16,
                Stretch = Stretch.Uniform
            };
            
            // Use a Path for the heart shape
            var path = new System.Windows.Shapes.Path
            {
                Data = Geometry.Parse("M12,21.35L10.55,20.03C5.4,15.36 2,12.27 2,8.5C2,5.41 4.42,3 7.5,3C9.24,3 10.91,3.81 12,5.08C13.09,3.81 14.76,3 16.5,3C19.58,3 22,5.41 22,8.5C22,12.27 18.6,15.36 13.45,20.03L12,21.35Z"),
                Fill = new SolidColorBrush(filled ? Colors.Red : Colors.Transparent),
                Stroke = new SolidColorBrush(Colors.Red),
                StrokeThickness = 1
            };
            
            viewbox.Child = path;
            return viewbox;
        }
        
        /// <summary>
        /// Safely logs an error across all platforms
        /// </summary>
        /// <summary>
        /// Logs an error message with optional exception details
        /// </summary>
        public static void LogError(string message, Exception ex = null)
        {
            Debug.WriteLine($"ERROR: {message}");
            
            if (ex != null)
            {
                Debug.WriteLine($"Exception: {ex.Message}");
                Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
            }
            
            // You can also log to a file or database using ErrorLogger if available
            try
            {
                if (ErrorLogger.IsInitialized)
                {
                    ErrorLogger.LogError(message, ex);
                }
            }
            catch
            {
                // Fallback if ErrorLogger isn't available
                Debug.WriteLine("Could not log to ErrorLogger");
            }
        }
        
        /// <summary>
        /// Safely finds a child element of the specified type regardless of platform
        /// </summary>
        public static T FindChildElement<T>(DependencyObject parent, string childName = null) where T : DependencyObject
        {
            // Return null if parent is null
            if (parent == null) return null;
            
            // Check if the parent itself is the requested type
            if (parent is T && (childName == null || (parent as FrameworkElement)?.Name == childName))
            {
                return parent as T;
            }
            
            // Get number of children in the visual tree
            int childCount = VisualTreeHelper.GetChildrenCount(parent);
            
            // Check each child
            for (int i = 0; i < childCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                
                // Check if this child is the requested type
                if (child is T && (childName == null || (child as FrameworkElement)?.Name == childName))
                {
                    return child as T;
                }
                
                // Recursively check this child's children
                var result = FindChildElement<T>(child, childName);
                if (result != null)
                {
                    return result;
                }
            }
            
            return null;
        }
    }
}
