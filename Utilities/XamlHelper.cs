using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Markup;

namespace DataGridNamespace.Utilities
{
    /// <summary>
    /// Helper class for cross-platform XAML compatibility
    /// </summary>
    public class XamlHelper
    {
        /// <summary>
        /// Creates a platform-independent icon element that works across platforms
        /// </summary>
        public static UIElement CreateIconElement(string iconType, Brush foreground, double width = 18, double height = 18)
        {
            // Create a TextBlock with a representative symbol as fallback
            TextBlock textBlock = new TextBlock
            {
                Text = GetSymbolForIconType(iconType),
                FontSize = Math.Max(width, height) * 0.8,
                Foreground = foreground,
                Width = width,
                Height = height,
                TextAlignment = TextAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            return textBlock;
        }

        /// <summary>
        /// Maps icon types to representative symbols for cross-platform compatibility
        /// </summary>
        private static string GetSymbolForIconType(string iconType)
        {
            switch (iconType.ToLowerInvariant())
            {
                case "information":
                    return "ℹ";
                case "filepdfbox":
                    return "📄";
                case "messagetext":
                    return "✉";
                case "heartoutline":
                    return "♡";
                case "heart":
                    return "♥";
                case "heartremove":
                    return "♥✕";
                case "viewdashboardoutline":
                    return "◫";
                case "bookopenvariant":
                    return "📚";
                case "accountmultipleoutline":
                    return "👥";
                case "accountcircleoutline":
                    return "👤";
                case "logout":
                    return "⎋";
                case "windowminimize":
                    return "─";
                case "windowmaximize":
                    return "□";
                case "close":
                    return "✕";
                default:
                    return "?";
            }
        }
    }
}
