using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using DataGridNamespace.Utilities;

namespace DataGridNamespace.Services
{
    /// <summary>
    /// Platform-independent PDF viewer service
    /// </summary>
    public static class PdfViewerService
    {
        /// <summary>
        /// Open a PDF file with the appropriate viewer for the current platform
        /// </summary>
        /// <param name="filePath">Path to the PDF file</param>
        /// <returns>True if successful</returns>
        public static bool OpenPdf(string filePath)
        {
            try
            {
                // Check if file exists
                if (!File.Exists(filePath))
                {
                    MessageService.Notify($"The file does not exist: {filePath}", NotificationType.Error);
                    return false;
                }
                
                // Check if it's a PDF file
                if (!filePath.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                {
                    MessageService.Notify("The file is not a PDF.", NotificationType.Error);
                    return false;
                }
                
                // Open using platform-specific method
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    return OpenPdfWindows(filePath);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    return OpenPdfMac(filePath);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    return OpenPdfLinux(filePath);
                }
                else
                {
                    // Generic fallback
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = filePath,
                        UseShellExecute = true
                    });
                    return true;
                }
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError($"Error opening PDF: {ex.Message}", ex, showMessageBox: true);
                return false;
            }
        }
        
        /// <summary>
        /// Open PDF on Windows
        /// </summary>
        private static bool OpenPdfWindows(string filePath)
        {
            // On Windows, we can use Process.Start with UseShellExecute
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = filePath,
                    UseShellExecute = true
                };
                Process.Start(psi);
                return true;
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError($"Error opening PDF on Windows: {ex.Message}", ex);
                
                // Fallback to default browser
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "cmd",
                        Arguments = $"/c start {filePath}",
                        UseShellExecute = true,
                        CreateNoWindow = true
                    });
                    return true;
                }
                catch (Exception fallbackEx)
                {
                    ErrorLogger.LogError($"Fallback failed: {fallbackEx.Message}", fallbackEx);
                    return false;
                }
            }
        }
        
        /// <summary>
        /// Open PDF on macOS
        /// </summary>
        private static bool OpenPdfMac(string filePath)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "open",
                    Arguments = $"\"{filePath}\"",
                    UseShellExecute = true,
                    CreateNoWindow = true
                });
                return true;
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError($"Error opening PDF on Mac: {ex.Message}", ex);
                return false;
            }
        }
        
        /// <summary>
        /// Open PDF on Linux
        /// </summary>
        private static bool OpenPdfLinux(string filePath)
        {
            // Try common PDF viewers in order
            string[] commands = { "xdg-open", "evince", "okular", "atril", "firefox", "google-chrome" };
            
            foreach (string command in commands)
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = command,
                        Arguments = $"\"{filePath}\"",
                        UseShellExecute = true,
                        CreateNoWindow = true
                    });
                    return true;
                }
                catch
                {
                    // Try the next command
                    continue;
                }
            }
            
            // If all commands failed, log error and return false
            ErrorLogger.LogError("Failed to open PDF on Linux. No suitable viewer found.");
            return false;
        }
        
        /// <summary>
        /// Open a PDF file from a URL
        /// </summary>
        public static bool OpenPdfFromUrl(string url)
        {
            try
            {
                // Validate URL
                if (string.IsNullOrWhiteSpace(url))
                {
                    MessageService.Notify("The URL is empty.", NotificationType.Error);
                    return false;
                }
                
                // Check if it's a local file URL
                if (url.StartsWith("file:///"))
                {
                    return OpenPdf(url.Substring(8));
                }
                
                // Open the URL in the default browser
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = url,
                        UseShellExecute = true
                    });
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "open",
                        Arguments = url,
                        UseShellExecute = true,
                        CreateNoWindow = true
                    });
                }
                else
                {
                    // Linux and others
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "xdg-open",
                        Arguments = url,
                        UseShellExecute = true,
                        CreateNoWindow = true
                    });
                }
                
                return true;
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError($"Error opening PDF URL: {ex.Message}", ex, showMessageBox: true);
                
                // Show message to user
                MessageBox.Show(
                    $"Could not open the URL: {url}\n\nError: {ex.Message}",
                    "Error Opening PDF",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                
                return false;
            }
        }
    }
}
