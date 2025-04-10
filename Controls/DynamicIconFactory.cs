using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using DataGridNamespace.Utilities;

namespace DataGridNamespace.Controls
{
    /// <summary>
    /// Factory class for dynamic creation of platform-independent icons
    /// </summary>
    public static class DynamicIconFactory
    {
        /// <summary>
        /// Creates a platform-independent icon element at runtime
        /// </summary>
        /// <param name="iconKind">Type of icon to create</param>
        /// <param name="foreground">Color of the icon</param>
        /// <param name="width">Width of the icon</param>
        /// <param name="height">Height of the icon</param>
        /// <returns>An appropriate UI element for the current platform</returns>
        public static UIElement CreateIcon(string iconKind, Brush foreground, double width = 18, double height = 18)
        {
            UIElement iconElement = null;
            
            // First try Windows-specific implementation if available
            #if WINDOWS
            try
            {
                // Use reflection to create PackIconMaterial on Windows platform
                var iconType = Type.GetType("MahApps.Metro.IconPacks.PackIconMaterial, MahApps.Metro.IconPacks");
                if (iconType != null)
                {
                    var kindType = Type.GetType("MahApps.Metro.IconPacks.PackIconMaterialKind, MahApps.Metro.IconPacks");
                    if (kindType != null && Enum.TryParse(kindType, iconKind, true, out object kindValue))
                    {
                        var icon = Activator.CreateInstance(iconType);
                        iconType.GetProperty("Kind").SetValue(icon, kindValue);
                        iconType.GetProperty("Width").SetValue(icon, width);
                        iconType.GetProperty("Height").SetValue(icon, height);
                        iconType.GetProperty("Foreground").SetValue(icon, foreground);
                        
                        iconElement = icon as UIElement;
                        
                        if (iconElement != null)
                        {
                            return iconElement;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                PlatformUtility.LogError($"Error creating PackIconMaterial: {ex.Message}", ex);
                // Fall through to platform-independent implementation
            }
            #endif
            
            // Create platform-independent icon as fallback
            switch (iconKind.ToLowerInvariant())
            {
                case "heart":
                case "heartoutline":
                    bool filled = iconKind.Equals("Heart", StringComparison.OrdinalIgnoreCase);
                    iconElement = CreateHeartIcon(filled, foreground, width, height);
                    break;
                    
                case "information":
                    iconElement = CreateInfoIcon(foreground, width, height);
                    break;
                    
                case "filepdfbox":
                    iconElement = CreatePdfIcon(foreground, width, height);
                    break;
                    
                case "messagetext":
                    iconElement = CreateMessageIcon(foreground, width, height);
                    break;
                    
                case "heartremove":
                    iconElement = CreateHeartRemoveIcon(foreground, width, height);
                    break;
                    
                default:
                    // Fallback to simple text representation
                    iconElement = CreateTextBasedIcon(iconKind, foreground, width, height);
                    break;
            }
            
            return iconElement;
        }
        
        /// <summary>
        /// Creates a heart icon
        /// </summary>
        private static UIElement CreateHeartIcon(bool filled, Brush foreground, double width, double height)
        {
            // Create a heart path shape
            Path path = new Path
            {
                Data = Geometry.Parse("M12,21.35L10.55,20.03C5.4,15.36 2,12.27 2,8.5C2,5.41 4.42,3 7.5,3C9.24,3 10.91,3.81 12,5.08C13.09,3.81 14.76,3 16.5,3C19.58,3 22,5.41 22,8.5C22,12.27 18.6,15.36 13.45,20.03L12,21.35Z"),
                Fill = filled ? foreground : Brushes.Transparent,
                Stroke = foreground,
                StrokeThickness = 1.5,
                Stretch = Stretch.Uniform,
                Width = width,
                Height = height
            };
            
            // Put it in a container for consistent sizing
            Grid container = new Grid
            {
                Width = width,
                Height = height
            };
            
            container.Children.Add(path);
            return container;
        }
        
        /// <summary>
        /// Creates an information icon
        /// </summary>
        private static UIElement CreateInfoIcon(Brush foreground, double width, double height)
        {
            Grid container = new Grid
            {
                Width = width,
                Height = height
            };
            
            // Circle
            Ellipse circle = new Ellipse
            {
                Fill = Brushes.Transparent,
                Stroke = foreground,
                StrokeThickness = 1.5,
                Width = width,
                Height = height
            };
            
            // "i" text
            TextBlock infoText = new TextBlock
            {
                Text = "i",
                FontWeight = FontWeights.Bold,
                FontSize = height * 0.6,
                Foreground = foreground,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            
            container.Children.Add(circle);
            container.Children.Add(infoText);
            
            return container;
        }
        
        /// <summary>
        /// Creates a PDF icon
        /// </summary>
        private static UIElement CreatePdfIcon(Brush foreground, double width, double height)
        {
            Grid container = new Grid
            {
                Width = width,
                Height = height
            };
            
            // Document shape
            Rectangle document = new Rectangle
            {
                Fill = Brushes.Transparent,
                Stroke = foreground,
                StrokeThickness = 1.5,
                Width = width * 0.8,
                Height = height,
                RadiusX = 2,
                RadiusY = 2,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            
            // PDF text
            TextBlock pdfText = new TextBlock
            {
                Text = "PDF",
                FontSize = height * 0.35,
                FontWeight = FontWeights.Bold,
                Foreground = foreground,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            
            container.Children.Add(document);
            container.Children.Add(pdfText);
            
            return container;
        }
        
        /// <summary>
        /// Creates a message icon
        /// </summary>
        private static UIElement CreateMessageIcon(Brush foreground, double width, double height)
        {
            // Create a message bubble path
            Path path = new Path
            {
                Data = Geometry.Parse("M20,2H4A2,2 0 0,0 2,4V22L6,18H20A2,2 0 0,0 22,16V4A2,2 0 0,0 20,2M20,16H6L4,18V4H20"),
                Fill = Brushes.Transparent,
                Stroke = foreground,
                StrokeThickness = 1.5,
                Stretch = Stretch.Uniform,
                Width = width,
                Height = height
            };
            
            // Put it in a container for consistent sizing
            Grid container = new Grid
            {
                Width = width,
                Height = height
            };
            
            container.Children.Add(path);
            return container;
        }
        
        /// <summary>
        /// Creates a heart with X (remove from favorites) icon
        /// </summary>
        private static UIElement CreateHeartRemoveIcon(Brush foreground, double width, double height)
        {
            Grid container = new Grid
            {
                Width = width,
                Height = height
            };
            
            // Heart
            Path heartPath = new Path
            {
                Data = Geometry.Parse("M12,21.35L10.55,20.03C5.4,15.36 2,12.27 2,8.5C2,5.41 4.42,3 7.5,3C9.24,3 10.91,3.81 12,5.08C13.09,3.81 14.76,3 16.5,3C19.58,3 22,5.41 22,8.5C22,12.27 18.6,15.36 13.45,20.03L12,21.35Z"),
                Fill = foreground,
                Stroke = foreground,
                StrokeThickness = 1,
                Stretch = Stretch.Uniform,
                Width = width * 0.9,
                Height = height * 0.9
            };
            
            // X shape
            Path xPath = new Path
            {
                Data = Geometry.Parse("M19,6.41L17.59,5L12,10.59L6.41,5L5,6.41L10.59,12L5,17.59L6.41,19L12,13.41L17.59,19L19,17.59L13.41,12L19,6.41Z"),
                Fill = Brushes.White,
                Stroke = Brushes.White,
                StrokeThickness = 0.5,
                Stretch = Stretch.Uniform,
                Width = width * 0.5,
                Height = height * 0.5,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(0, 0, 0, 0)
            };
            
            container.Children.Add(heartPath);
            container.Children.Add(xPath);
            
            return container;
        }
        
        /// <summary>
        /// Creates a simple text-based icon as last resort fallback
        /// </summary>
        private static UIElement CreateTextBasedIcon(string iconKind, Brush foreground, double width, double height)
        {
            // Map icon names to Unicode symbols
            string iconText = GetSymbolForIconKind(iconKind);
            
            // Create text block
            TextBlock textBlock = new TextBlock
            {
                Text = iconText,
                FontSize = height * 0.7,
                Foreground = foreground,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                TextAlignment = TextAlignment.Center,
                Width = width,
                Height = height
            };
            
            return textBlock;
        }
        
        /// <summary>
        /// Maps icon kinds to Unicode symbols for text representation
        /// </summary>
        private static string GetSymbolForIconKind(string iconKind)
        {
            switch (iconKind.ToLowerInvariant())
            {
                case "heart": return "♥";
                case "heartoutline": return "♡";
                case "information": return "ℹ";
                case "filepdfbox": return "📄";
                case "messagetext": return "✉";
                case "heartremove": return "♥✕";
                case "home": return "⌂";
                case "viewdashboardoutline": return "◫";
                case "bookopenvariant": return "📚";
                case "accountmultipleoutline": return "👥";
                case "accountcircleoutline": return "👤";
                case "logout": return "⎋";
                case "windowminimize": return "─";
                case "windowmaximize": return "□";
                case "close": return "✖";
                case "search": return "🔍";
                case "refresh": return "↻";
                case "plus": return "+";
                case "minus": return "−";
                case "check": return "✓";
                case "arrowleft": return "←";
                case "arrowright": return "→";
                case "arrowup": return "↑";
                case "arrowdown": return "↓";
                default: return "•";
            }
        }
    }
}
