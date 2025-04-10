using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace DataGridNamespace.Utilities
{
    /// <summary>
    /// Platform-agnostic icon renderer that works across all platforms
    /// </summary>
    public static class IconRenderer
    {
        /// <summary>
        /// Creates a platform-independent heart icon
        /// </summary>
        public static UIElement CreateHeartIcon(bool filled)
        {
            Path path = new Path
            {
                Data = Geometry.Parse("M12,21.35L10.55,20.03C5.4,15.36 2,12.27 2,8.5C2,5.41 4.42,3 7.5,3C9.24,3 10.91,3.81 12,5.08C13.09,3.81 14.76,3 16.5,3C19.58,3 22,5.41 22,8.5C22,12.27 18.6,15.36 13.45,20.03L12,21.35Z"),
                Fill = new SolidColorBrush(filled ? Colors.Red : Colors.Transparent),
                Stroke = new SolidColorBrush(Colors.Red),
                StrokeThickness = 1,
                Stretch = Stretch.Uniform
            };

            Viewbox viewbox = new Viewbox
            {
                Width = 18,
                Height = 18,
                Child = path
            };

            return viewbox;
        }

        /// <summary>
        /// Creates a platform-independent information icon
        /// </summary>
        public static UIElement CreateInfoIcon()
        {
            Grid grid = new Grid
            {
                Width = 18,
                Height = 18
            };

            Ellipse circle = new Ellipse
            {
                Width = 18,
                Height = 18,
                Fill = Brushes.Transparent,
                Stroke = new SolidColorBrush(Color.FromRgb(0, 128, 128)), // #008080
                StrokeThickness = 1.5
            };

            TextBlock infoText = new TextBlock
            {
                Text = "i",
                FontWeight = FontWeights.Bold,
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(0, 128, 128)),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            grid.Children.Add(circle);
            grid.Children.Add(infoText);

            return grid;
        }

        /// <summary>
        /// Creates a platform-independent PDF icon
        /// </summary>
        public static UIElement CreatePdfIcon()
        {
            Grid grid = new Grid
            {
                Width = 18,
                Height = 18
            };

            // Document shape
            Rectangle rect = new Rectangle
            {
                Width = 14,
                Height = 18,
                Fill = Brushes.Transparent,
                Stroke = new SolidColorBrush(Color.FromRgb(192, 57, 43)), // #C0392B
                StrokeThickness = 1.5,
                RadiusX = 2,
                RadiusY = 2
            };

            // PDF text
            TextBlock pdfText = new TextBlock
            {
                Text = "PDF",
                FontWeight = FontWeights.Bold,
                FontSize = 8,
                Foreground = new SolidColorBrush(Color.FromRgb(192, 57, 43)),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            grid.Children.Add(rect);
            grid.Children.Add(pdfText);

            return grid;
        }

        /// <summary>
        /// Creates a platform-independent message icon
        /// </summary>
        public static UIElement CreateMessageIcon()
        {
            Path path = new Path
            {
                Data = Geometry.Parse("M20,2H4A2,2 0 0,0 2,4V22L6,18H20A2,2 0 0,0 22,16V4A2,2 0 0,0 20,2M20,16H6L4,18V4H20"),
                Fill = Brushes.Transparent,
                Stroke = new SolidColorBrush(Color.FromRgb(39, 174, 96)), // #27AE60
                StrokeThickness = 1.5,
                Stretch = Stretch.Uniform
            };

            Viewbox viewbox = new Viewbox
            {
                Width = 18,
                Height = 18,
                Child = path
            };

            return viewbox;
        }

        /// <summary>
        /// Creates a simple icon using Unicode characters as a fallback for platforms without vector icons
        /// </summary>
        public static TextBlock CreateSimpleTextIcon(string iconType)
        {
            string iconText;
            Brush iconColor;

            switch (iconType.ToLowerInvariant())
            {
                case "heart":
                    iconText = "♥";
                    iconColor = new SolidColorBrush(Colors.Red);
                    break;
                case "heartoutline":
                    iconText = "♡";
                    iconColor = new SolidColorBrush(Colors.Red);
                    break;
                case "information":
                    iconText = "ℹ";
                    iconColor = new SolidColorBrush(Color.FromRgb(0, 128, 128));
                    break;
                case "filepdfbox":
                    iconText = "📄";
                    iconColor = new SolidColorBrush(Color.FromRgb(192, 57, 43));
                    break;
                case "messagetext":
                    iconText = "✉";
                    iconColor = new SolidColorBrush(Color.FromRgb(39, 174, 96));
                    break;
                case "heartremove":
                    iconText = "♥✕";
                    iconColor = new SolidColorBrush(Colors.Red);
                    break;
                default:
                    iconText = "•";
                    iconColor = Brushes.Gray;
                    break;
            }

            return new TextBlock
            {
                Text = iconText,
                FontSize = 14,
                Foreground = iconColor,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                TextAlignment = TextAlignment.Center
            };
        }
    }
}
