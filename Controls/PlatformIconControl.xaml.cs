using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using DataGridNamespace.Utilities;

namespace DataGridNamespace.Controls
{
    /// <summary>
    /// Platform-independent icon control that works across all platforms
    /// </summary>
    public partial class PlatformIconControl : UserControl
    {
        // Dependency property for icon kind
        public static readonly DependencyProperty IconKindProperty =
            DependencyProperty.Register(
                "IconKind", typeof(string), typeof(PlatformIconControl),
                new PropertyMetadata("Information", OnIconKindChanged));

        // Dependency property for icon foreground
        public static readonly DependencyProperty IconForegroundProperty =
            DependencyProperty.Register(
                "IconForeground", typeof(Brush), typeof(PlatformIconControl),
                new PropertyMetadata(Brushes.Black, OnForegroundChanged));

        // Properties
        public string IconKind
        {
            get { return (string)GetValue(IconKindProperty); }
            set { SetValue(IconKindProperty, value); }
        }

        public Brush IconForeground
        {
            get { return (Brush)GetValue(IconForegroundProperty); }
            set { SetValue(IconForegroundProperty, value); }
        }

        // Constructor
        public PlatformIconControl()
        {
            InitializeComponent();
            UpdateIcon();
        }

        // Static handlers for property changes
        private static void OnIconKindChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PlatformIconControl control)
            {
                control.UpdateIcon();
            }
        }

        private static void OnForegroundChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PlatformIconControl control)
            {
                control.UpdateIcon();
            }
        }

        // Update the icon based on current property values
        private void UpdateIcon()
        {
            try
            {
                // Create an appropriate icon element based on the platform
                UIElement iconElement;

                #if WINDOWS
                // Try to create a PackIconMaterial if we're on Windows
                try
                {
                    // Use reflection to create a PackIconMaterial
                    var iconType = Type.GetType("MahApps.Metro.IconPacks.PackIconMaterial, MahApps.Metro.IconPacks");
                    if (iconType != null)
                    {
                        var icon = Activator.CreateInstance(iconType);
                        
                        // Set Kind property
                        var kindType = Type.GetType("MahApps.Metro.IconPacks.PackIconMaterialKind, MahApps.Metro.IconPacks");
                        if (kindType != null)
                        {
                            var kindValue = Enum.Parse(kindType, IconKind, true);
                            iconType.GetProperty("Kind").SetValue(icon, kindValue);
                            
                            // Set other properties
                            iconType.GetProperty("Foreground").SetValue(icon, IconForeground);
                            iconType.GetProperty("Width").SetValue(icon, 18.0);
                            iconType.GetProperty("Height").SetValue(icon, 18.0);
                            
                            iconElement = (UIElement)icon;
                            IconContainer.Content = iconElement;
                            return;
                        }
                    }
                }
                catch (Exception)
                {
                    // Fall through to the platform-independent solution
                }
                #endif

                // Platform-independent implementation as fallback
                iconElement = XamlHelper.CreateIconElement(IconKind, IconForeground);
                IconContainer.Content = iconElement;
            }
            catch (Exception ex)
            {
                // If all else fails, use a simple TextBlock
                IconContainer.Content = new TextBlock
                {
                    Text = "•",
                    Foreground = IconForeground,
                    Style = (Style)Resources["FallbackTextBlockStyle"]
                };
                
                // Log the error
                PlatformUtility.LogError($"Error creating icon for kind {IconKind}", ex);
            }
        }
    }
}
