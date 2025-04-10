using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using DataGridNamespace.Utilities;

namespace DataGridNamespace.Services
{
    /// <summary>
    /// Provides platform-independent message dialogs and notifications
    /// </summary>
    public static class MessageService
    {
        private static Window _notificationWindow;
        
        /// <summary>
        /// Show a message box with custom styling that works across platforms
        /// </summary>
        public static MessageBoxResult ShowMessage(string message, string title = "Message", 
            MessageBoxButton buttons = MessageBoxButton.OK, 
            MessageBoxImage icon = MessageBoxImage.Information)
        {
            try
            {
                return MessageBox.Show(message, title, buttons, icon);
            }
            catch (Exception ex)
            {
                // Log error but don't crash
                PlatformUtility.LogError($"Error showing message: {ex.Message}", ex);
                
                // Fallback to Console output if MessageBox fails
                Console.WriteLine($"{title}: {message}");
                return MessageBoxResult.None;
            }
        }
        
        /// <summary>
        /// Show a toast notification that appears and fades out
        /// </summary>
        public static void ShowToast(string message, ToastType type = ToastType.Information, int durationMs = 3000)
        {
            try
            {
                // Run on UI thread
                Application.Current.Dispatcher.Invoke(() =>
                {
                    // Get background and foreground colors based on toast type
                    var (background, foreground) = GetToastColors(type);
                    
                    // Create the toast content
                    Border toastBorder = new Border
                    {
                        Background = background,
                        BorderBrush = new SolidColorBrush(Colors.Gray),
                        BorderThickness = new Thickness(1),
                        CornerRadius = new CornerRadius(8),
                        Padding = new Thickness(15, 10, 15, 10),
                        MaxWidth = 400,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Bottom,
                        Margin = new Thickness(0, 0, 0, 50),
                        Effect = new System.Windows.Media.Effects.DropShadowEffect
                        {
                            Color = Colors.Black,
                            Direction = 320,
                            ShadowDepth = 5,
                            Opacity = 0.3,
                            BlurRadius = 10
                        }
                    };
                    
                    // Message text
                    TextBlock textBlock = new TextBlock
                    {
                        Text = message,
                        Foreground = foreground,
                        TextWrapping = TextWrapping.Wrap,
                        FontSize = 14,
                        TextAlignment = TextAlignment.Center
                    };
                    
                    toastBorder.Child = textBlock;
                    
                    // Create or reuse notification window
                    if (_notificationWindow == null || !_notificationWindow.IsLoaded)
                    {
                        _notificationWindow = new Window
                        {
                            WindowStyle = WindowStyle.None,
                            AllowsTransparency = true,
                            Background = Brushes.Transparent,
                            Topmost = true,
                            ShowInTaskbar = false,
                            SizeToContent = SizeToContent.WidthAndHeight,
                            ResizeMode = ResizeMode.NoResize
                        };
                    }
                    
                    // Position the window
                    _notificationWindow.Content = toastBorder;
                    
                    // Get the main window to position the toast relative to it
                    Window mainWindow = Application.Current.MainWindow;
                    if (mainWindow != null && mainWindow.IsLoaded)
                    {
                        _notificationWindow.Left = mainWindow.Left + (mainWindow.Width - toastBorder.MaxWidth) / 2;
                        _notificationWindow.Top = mainWindow.Top + mainWindow.Height - 120;
                    }
                    else
                    {
                        // Position in the center of the screen if no main window
                        double screenWidth = SystemParameters.PrimaryScreenWidth;
                        double screenHeight = SystemParameters.PrimaryScreenHeight;
                        _notificationWindow.Left = (screenWidth - toastBorder.MaxWidth) / 2;
                        _notificationWindow.Top = screenHeight - 120;
                    }
                    
                    // Show the window
                    _notificationWindow.Show();
                    
                    // Create fade-in animation
                    DoubleAnimation fadeIn = new DoubleAnimation
                    {
                        From = 0,
                        To = 1,
                        Duration = TimeSpan.FromMilliseconds(300)
                    };
                    
                    // Create fade-out animation
                    DoubleAnimation fadeOut = new DoubleAnimation
                    {
                        From = 1,
                        To = 0,
                        Duration = TimeSpan.FromMilliseconds(500),
                        BeginTime = TimeSpan.FromMilliseconds(durationMs)
                    };
                    
                    // When fade-out completes, close the window
                    fadeOut.Completed += (s, e) => _notificationWindow.Close();
                    
                    // Apply animations
                    _notificationWindow.BeginAnimation(UIElement.OpacityProperty, fadeIn);
                    _notificationWindow.BeginAnimation(UIElement.OpacityProperty, fadeOut);
                });
            }
            catch (Exception ex)
            {
                // Log error but don't crash
                PlatformUtility.LogError($"Error showing toast notification: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// Get background and foreground colors for different toast types
        /// </summary>
        private static (Brush background, Brush foreground) GetToastColors(ToastType type)
        {
            return type switch
            {
                ToastType.Success => (new SolidColorBrush(Color.FromRgb(46, 204, 113)), Brushes.White),
                ToastType.Error => (new SolidColorBrush(Color.FromRgb(231, 76, 60)), Brushes.White),
                ToastType.Warning => (new SolidColorBrush(Color.FromRgb(241, 196, 15)), Brushes.Black),
                _ => (new SolidColorBrush(Color.FromRgb(52, 152, 219)), Brushes.White) // Information
            };
        }
        
        /// <summary>
        /// Display message to user with automatic selection of the most appropriate method
        /// (toast for non-critical information, message box for important notices)
        /// </summary>
        public static void Notify(string message, NotificationType type = NotificationType.Information)
        {
            switch (type)
            {
                case NotificationType.Success:
                    ShowToast(message, ToastType.Success);
                    break;
                    
                case NotificationType.Error:
                    ShowMessage(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    break;
                    
                case NotificationType.Warning:
                    ShowToast(message, ToastType.Warning, 4000);
                    break;
                    
                case NotificationType.Question:
                    ShowMessage(message, "Question", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    break;
                    
                case NotificationType.Information:
                default:
                    ShowToast(message, ToastType.Information);
                    break;
            }
        }
    }
    
    /// <summary>
    /// Types of toast notifications
    /// </summary>
    public enum ToastType
    {
        Information,
        Success,
        Warning,
        Error
    }
    
    /// <summary>
    /// Types of notifications that determine delivery method
    /// </summary>
    public enum NotificationType
    {
        Information,
        Success,
        Warning,
        Error,
        Question
    }
}
