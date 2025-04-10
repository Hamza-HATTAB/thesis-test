using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace DataGridNamespace.Utilities
{
    /// <summary>
    /// Factory class for creating platform-independent buttons with icons
    /// </summary>
    public static class ButtonFactory
    {
        /// <summary>
        /// Creates a favorite button that works across all platforms
        /// </summary>
        /// <param name="isFavorite">Whether the initial state should be favorite</param>
        /// <param name="clickHandler">Event handler for button click</param>
        /// <param name="tag">Data to associate with the button</param>
        /// <returns>A button with appropriate styling</returns>
        public static Button CreateFavoriteButton(bool isFavorite, RoutedEventHandler clickHandler, object tag = null)
        {
            var button = new Button
            {
                Width = 30,
                Height = 30,
                Margin = new Thickness(2),
                Background = new SolidColorBrush(isFavorite ? Color.FromRgb(255, 200, 200) : Color.FromRgb(240, 240, 240)),
                BorderBrush = new SolidColorBrush(isFavorite ? Colors.Red : Colors.Gray),
                Tag = tag,
                ToolTip = isFavorite ? "Remove from favorites" : "Add to favorites",
                Name = "FavoriteButton"
            };

            // Create platform-independent heart icon
            var heartIcon = new Path
            {
                Data = Geometry.Parse("M12,21.35L10.55,20.03C5.4,15.36 2,12.27 2,8.5C2,5.41 4.42,3 7.5,3C9.24,3 10.91,3.81 12,5.08C13.09,3.81 14.76,3 16.5,3C19.58,3 22,5.41 22,8.5C22,12.27 18.6,15.36 13.45,20.03L12,21.35Z"),
                Fill = new SolidColorBrush(isFavorite ? Colors.Red : Colors.Transparent),
                Stroke = new SolidColorBrush(Colors.Red),
                StrokeThickness = 1,
                Width = 16,
                Height = 16,
                Stretch = Stretch.Uniform,
                Name = "FavoriteIcon"
            };

            // Wrap in a viewbox for consistency
            var viewbox = new Viewbox
            {
                Width = 16,
                Height = 16,
                Child = heartIcon
            };

            button.Content = viewbox;
            
            if (clickHandler != null)
            {
                button.Click += clickHandler;
            }

            return button;
        }

        /// <summary>
        /// Creates an action button with a specific icon and functionality
        /// </summary>
        /// <param name="iconType">Type of icon (info, pdf, message, delete)</param>
        /// <param name="clickHandler">Event handler for button click</param>
        /// <param name="tag">Data to associate with the button</param>
        /// <param name="tooltip">Tooltip text</param>
        /// <returns>A button with appropriate styling</returns>
        public static Button CreateActionButton(string iconType, RoutedEventHandler clickHandler, object tag = null, string tooltip = null)
        {
            var button = new Button
            {
                Width = 30,
                Height = 30,
                Margin = new Thickness(2),
                Background = new SolidColorBrush(Color.FromRgb(240, 240, 240)),
                BorderBrush = new SolidColorBrush(Colors.Gray),
                Tag = tag,
                ToolTip = tooltip
            };
            
            // Set icon based on type
            Path iconPath = new Path
            {
                Width = 16,
                Height = 16,
                Stretch = Stretch.Uniform
            };
            
            switch (iconType.ToLower())
            {
                case "info":
                    iconPath.Data = Geometry.Parse("M12,2A10,10 0 0,1 22,12A10,10 0 0,1 12,22A10,10 0 0,1 2,12A10,10 0 0,1 12,2M12,4A8,8 0 0,0 4,12A8,8 0 0,0 12,20A8,8 0 0,0 20,12A8,8 0 0,0 12,4M11,16.5L6.5,12L7.91,10.59L11,13.67L16.59,8.09L18,9.5L11,16.5Z");
                    iconPath.Fill = new SolidColorBrush(Colors.CornflowerBlue);
                    button.ToolTip = tooltip ?? "View Details";
                    break;
                case "pdf":
                    iconPath.Data = Geometry.Parse("M8,2H16A2,2 0 0,1 18,4V20A2,2 0 0,1 16,22H8A2,2 0 0,1 6,20V4A2,2 0 0,1 8,2M12,4A1.5,1.5 0 0,0 10.5,5.5A1.5,1.5 0 0,0 12,7A1.5,1.5 0 0,0 13.5,5.5A1.5,1.5 0 0,0 12,4M7,9V10H17V9H7M7,11V12H17V11H7M7,13V14H14V13H7Z");
                    iconPath.Fill = new SolidColorBrush(Colors.Crimson);
                    button.ToolTip = tooltip ?? "View PDF";
                    break;
                case "message":
                    iconPath.Data = Geometry.Parse("M20,2H4A2,2 0 0,0 2,4V22L6,18H20A2,2 0 0,0 22,16V4A2,2 0 0,0 20,2M20,16H6L4,18V4H20V16Z");
                    iconPath.Fill = new SolidColorBrush(Colors.ForestGreen);
                    button.ToolTip = tooltip ?? "Send Message";
                    break;
                case "delete":
                    iconPath.Data = Geometry.Parse("M19,4H15.5L14.5,3H9.5L8.5,4H5V6H19M6,19A2,2 0 0,0 8,21H16A2,2 0 0,0 18,19V7H6V19Z");
                    iconPath.Fill = new SolidColorBrush(Colors.DarkRed);
                    button.ToolTip = tooltip ?? "Delete";
                    break;
            }
            
            // Wrap in a viewbox for consistency
            var viewbox = new Viewbox
            {
                Width = 16,
                Height = 16,
                Child = iconPath
            };
            
            button.Content = viewbox;
            
            if (clickHandler != null)
            {
                button.Click += clickHandler;
            }
            
            return button;
        }
        
        /// <summary>
        /// Updates the favorite status of a platform-independent favorite button
        /// </summary>
        /// <param name="button">The button to update</param>
        /// <param name="isFavorite">Whether the item is a favorite</param>
        public static void UpdateFavoriteButtonState(Button button, bool isFavorite)
        {
            if (button == null) return;
            
            button.Background = new SolidColorBrush(isFavorite ? Color.FromRgb(255, 200, 200) : Color.FromRgb(240, 240, 240));
            button.BorderBrush = new SolidColorBrush(isFavorite ? Colors.Red : Colors.Gray);
            button.ToolTip = isFavorite ? "Remove from favorites" : "Add to favorites";
            
            // Update the icon if it exists
            if (button.Content is Viewbox viewbox && viewbox.Child is Path heartPath)
            {
                heartPath.Fill = new SolidColorBrush(isFavorite ? Colors.Red : Colors.Transparent);
            }
        }
    }
}
