using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Diagnostics;
using MySql.Data.MySqlClient;
using ThesesModels;
using System.IO;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Data;
using DataGridNamespace.Models;
using MahApps.Metro.IconPacks;
using DataGridNamespace.Utilities;

namespace DataGridNamespace.Admin
{
    /// <summary>
    /// صفحة المفضلة مع واجهة مستخدم حديثة وميزات متقدمة
    /// </summary>
    public partial class FavoritesView : UserControl
    {
        // Event handler for viewing PDF of a thesis is implemented later in the file

        // Event handler for viewing thesis details is implemented later in the file

        // Event handler for sending a message about a thesis is implemented later in the file

        // Event handler for removing a thesis from favorites is implemented below (line ~1010)
        // تعريف المتغيرات الأساسية
        private int currentPage = 1; // الصفحة الحالية
        private int itemsPerPage = 20; // عدد العناصر في الصفحة
        private int totalItems = 0; // إجمالي عدد العناصر
        private string currentSearchText = ""; // نص البحث الحالي
        private string currentThesisType = "All"; // نوع الأطروحة المحدد للتصفية
        private int currentUserId; // معرف المستخدم الحالي
        // Using DatabaseConnection.GetConnection() for centralized connection management

        /// <summary>
        /// إنشاء مثيل جديد من صفحة المفضلة
        /// </summary>
        public FavoritesView()
        {
            try
            {
                InitializeComponent();

                // Add Loaded event handler to ensure controls are properly initialized
                this.Loaded += FavoritesView_Loaded;

                // Get current user ID from Session
                if (Session.IsLoggedIn)
                {
                    currentUserId = Session.CurrentUserId;
                }
                else
                {
                    // If not logged in, use a default value or handle appropriately
                    currentUserId = 1;
                    MessageBox.Show("User session not found. Please log in to view your favorites.",
                                   "Session Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage("Error initializing FavoritesView", ex);
            }
        }



        /// <summary>
        /// Event handler called when the UserControl is loaded
        /// </summary>
        private void FavoritesView_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Ensure FavoritesDataGrid is accessible before trying to use it
                if (FavoritesDataGrid == null)
                {
                    Debug.WriteLine("WARNING: FavoritesDataGrid was null on load");
                    // Wait for the control to be properly initialized
                    Dispatcher.InvokeAsync(() => {
                        LoadFavorites(); // Load favorites data directly
                    }, System.Windows.Threading.DispatcherPriority.Loaded);
                    return;
                }
                
                // The InitializeDatabase method is obsolete - use DatabaseConnection.GetConnection()
                LoadFavorites(); // Load favorites data directly
            }
            catch (Exception ex)
            {
                ShowErrorMessage("Error loading favorites view", ex);
            }
        }

        /// <summary>
        /// إنشاء مثيل جديد من صفحة المفضلة مع تحديد معرف المستخدم
        /// </summary>
        /// <param name="userId">معرف المستخدم الحالي</param>
        public FavoritesView(int userId)
        {
            try
            {
                InitializeComponent();
                
                // Add Loaded event handler to ensure controls are properly initialized
                this.Loaded += FavoritesView_Loaded;
                
                // Validate user ID
                if (userId <= 0)
                {
                    MessageBox.Show("Invalid user ID. Please log in again to view favorites.", 
                                   "User ID Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    
                    // Try to get ID from session as fallback
                    if (Session.IsLoggedIn)
                    {
                        userId = Session.CurrentUserId;
                    }
                    else
                    {
                        userId = 1; // Default fallback
                    }
                }
                
                currentUserId = userId; // تعيين معرف المستخدم الحالي
                // Database connection is now managed centrally by DatabaseConnection.GetConnection()
                LoadFavorites(); // تحميل المفضلة
            }
            catch (Exception ex)
            {
                ShowErrorMessage("Error initializing FavoritesView", ex);
            }
        }

        // InitializeDatabase method has been removed
        // Database connections are now managed centrally through DatabaseConnection.GetConnection()

        /// <summary>
        /// تحميل الأطروحات المفضلة من قاعدة البيانات مع دعم البحث والتصفية والترقيم الصفحي
        /// </summary>
        private void LoadFavorites()
        {
            try
            {
                // Use a new connection each time to avoid disposed connection issues
                using (var connection = DatabaseConnection.GetConnection())
                {
                    // Check if connection is valid
                    if (connection == null || connection.State != ConnectionState.Open)
                    {
                        MessageBox.Show("Cannot connect to database. Please check your network connection and try again.",
                            "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    // استعلام لحساب إجمالي عدد العناصر
                    string countQuery = "SELECT COUNT(*) FROM favoris f " +
                                       "JOIN theses t ON f.these_id = t.Id " +
                                       "JOIN users u ON t.user_id = u.Id " +
                                       "WHERE f.user_id = @UserId";

                    // استعلام لجلب البيانات مع دعم الترقيم الصفحي
                    string query = "SELECT t.*, u.Nom AS Auteur, f.id AS FavorisId FROM favoris f " +
                                  "JOIN theses t ON f.these_id = t.Id " +
                                  "JOIN users u ON t.user_id = u.Id " +
                                  "WHERE f.user_id = @UserId";

                    // إضافة شروط التصفية حسب النوع إذا تم تحديده
                    if (currentThesisType != "All")
                    {
                        countQuery += " AND t.Type = @Type";
                        query += " AND t.Type = @Type";
                    }

                    // إضافة شروط البحث النصي إذا تم إدخاله
                    if (!string.IsNullOrEmpty(currentSearchText))
                    {
                        string searchCondition = " (t.Titre LIKE @SearchText OR u.Nom LIKE @SearchText)";
                        countQuery += " AND " + searchCondition;
                        query += " AND " + searchCondition;
                    }

                    // إضافة ترتيب وترقيم صفحي - تغيير من ترتيب تنازلي إلى تصاعدي
                    query += " ORDER BY f.id ASC LIMIT @Offset, @Limit";

                    // الحصول على إجمالي عدد العناصر أولاً
                    using (MySqlCommand countCmd = new MySqlCommand(countQuery, connection))
                    {
                        countCmd.Parameters.AddWithValue("@UserId", currentUserId);

                        if (currentThesisType != "All")
                        {
                            countCmd.Parameters.AddWithValue("@Type", currentThesisType);
                        }

                        if (!string.IsNullOrEmpty(currentSearchText))
                        {
                            countCmd.Parameters.AddWithValue("@SearchText", "%" + currentSearchText + "%");
                        }

                        totalItems = Convert.ToInt32(countCmd.ExecuteScalar());
                    }

                    // جلب البيانات مع الترقيم الصفحي
                    using (MySqlCommand cmd = new MySqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@UserId", currentUserId);

                        if (currentThesisType != "All")
                        {
                            cmd.Parameters.AddWithValue("@Type", currentThesisType);
                        }

                        if (!string.IsNullOrEmpty(currentSearchText))
                        {
                            cmd.Parameters.AddWithValue("@SearchText", "%" + currentSearchText + "%");
                        }

                        cmd.Parameters.AddWithValue("@Offset", (currentPage - 1) * itemsPerPage);
                        cmd.Parameters.AddWithValue("@Limit", itemsPerPage);

                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            List<Theses> favoritesList = new List<Theses>();

                            while (reader.Read())
                            {
                                var thesis = new Theses
                                {
                                    Id = reader.GetInt32("id"),
                                    Titre = reader.GetString("titre"),
                                    Resume = reader.GetString("Resume"),
                                    Speciality = reader.GetString("speciality"),
                                    Type = reader.GetString("Type"),
                                    Mots_cles = reader.GetString("mots_cles"),
                                    Annee = reader.GetDateTime("annee"),
                                    Fichier = reader.GetString("fichier"),
                                    UserId = reader.GetInt32("user_id"),
                                    Auteur = reader.GetString("Auteur"),
                                    FavorisId = reader.GetInt32("FavorisId") // تخزين معرف المفضلة للحذف لاحقاً
                                };

                                favoritesList.Add(thesis);
                            }

                            FavoritesDataGrid.ItemsSource = favoritesList;
                        }
                    }

                    // تحديث معلومات الترقيم الصفحي
                    UpdatePaginationInfo();
                    UpdatePaginationButtons();
                }
            }
            catch (MySqlException ex)
            {
                // معالجة أخطاء قاعدة البيانات بشكل خاص
                HandleDatabaseError(ex);
            }
            catch (Exception ex)
            {
                ShowErrorMessage("Error loading favorites", ex);
            }
        }

        /// <summary>
        /// معالجة أخطاء قاعدة البيانات
        /// </summary>
        private void HandleDatabaseError(MySqlException ex)
        {
            switch (ex.Number)
            {
                case 0: // Cannot connect to server
                    ShowErrorMessage("Cannot connect to database server. Contact administrator", ex);
                    break;
                case 1042: // Unable to connect to any of the specified MySQL hosts
                    ShowErrorMessage("Database server is not available. Check network connection", ex);
                    break;
                case 1045: // Invalid username/password
                    ShowErrorMessage("Invalid database credentials", ex);
                    break;
                default:
                    ShowErrorMessage($"Database error (Code: {ex.Number})", ex);
                    break;
            }

            // Try to initialize a new connection instead of reusing possibly disposed one
            try
            {
                // Use database connection factory to get a fresh connection
                using (var newConnection = DatabaseConnection.GetConnection())
                {
                    // Just testing connection
                    if (newConnection != null && newConnection.State == ConnectionState.Open)
                    {
                        // Connection successful, close it as we're just testing
                        newConnection.Close();
                    }
                }
            }
            catch
            {
                // تجاهل الخطأ هنا لأننا قمنا بالفعل بعرض رسالة الخطأ
            }
        }

        /// <summary>
        /// تحديث معلومات الترقيم الصفحي
        /// </summary>
        private void UpdatePaginationInfo()
        {
            int startItem = (currentPage - 1) * itemsPerPage + 1;
            int endItem = Math.Min(currentPage * itemsPerPage, totalItems);

            if (totalItems == 0)
            {
                startItem = 0;
                endItem = 0;
            }

            PaginationInfoTextBlock.Text = $"Showing {startItem}-{endItem} of {totalItems} favorite theses";
        }

        /// <summary>
        /// تحديث حالة أزرار الترقيم الصفحي
        /// </summary>
        private void UpdatePaginationButtons()
        {
            PreviousButton.IsEnabled = currentPage > 1;
            NextButton.IsEnabled = currentPage * itemsPerPage < totalItems;
        }

        /// <summary>
        /// معالج حدث تغيير نص البحث
        /// </summary>
        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                currentSearchText = SearchTextBox.Text.Trim();
                currentPage = 1; // إعادة التعيين إلى الصفحة الأولى عند البحث
                LoadFavorites();
            }
            catch (Exception ex)
            {
                ShowErrorMessage("Error during search", ex);
            }
        }

        /// <summary>
        /// معالج حدث تغيير نوع الأطروحة
        /// </summary>
        private void TypeFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (TypeFilterComboBox.SelectedItem != null)
                {
                    ComboBoxItem selectedItem = (ComboBoxItem)TypeFilterComboBox.SelectedItem;
                    currentThesisType = selectedItem.Content.ToString();

                    if (currentThesisType == "All Types")
                    {
                        currentThesisType = "All";
                    }

                    currentPage = 1; // إعادة التعيين إلى الصفحة الأولى عند تغيير التصفية
                    LoadFavorites();
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage("Error during type filtering", ex);
            }
        }

        /// <summary>
        /// معالج حدث النقر على زر الصفحة السابقة
        /// </summary>
        private void PreviousButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (currentPage > 1)
                {
                    currentPage--;
                    LoadFavorites();
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage("Error navigating to previous page", ex);
            }
        }

        /// <summary>
        /// معالج حدث النقر على زر الصفحة التالية
        /// </summary>
        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (currentPage * itemsPerPage < totalItems)
                {
                    currentPage++;
                    LoadFavorites();
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage("Error navigating to next page", ex);
            }
        }

        /// <summary>
        /// معالج حدث النقر على زر التحديث
        /// </summary>
        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                LoadFavorites();
            }
            catch (Exception ex)
            {
                ShowErrorMessage("Error refreshing data", ex);
            }
        }

        /// <summary>
        /// معالج حدث النقر على زر عرض التفاصيل
        /// </summary>
        private void ViewDetails_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button button && button.Tag is Theses thesis)
                {
                    // إنشاء نافذة عرض تفاصيل الأطروحة
                    var detailsWindow = new Window
                    {
                        Title = "Thesis Details",
                        Width = 600,
                        Height = 500,
                        WindowStartupLocation = WindowStartupLocation.CenterScreen,
                        ResizeMode = ResizeMode.NoResize
                    };

                    var grid = new Grid();
                    grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
                    grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                    grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });

                    var titleTextBlock = new TextBlock
                    {
                        Text = "Thesis Details",
                        Margin = new Thickness(20),
                        FontSize = 24,
                        FontWeight = FontWeights.SemiBold,
                        Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#008080"))
                    };
                    Grid.SetRow(titleTextBlock, 0);

                    var detailsGrid = new Grid();
                    detailsGrid.Margin = new Thickness(20, 0, 20, 0);
                    detailsGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
                    detailsGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
                    detailsGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
                    detailsGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
                    detailsGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
                    detailsGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                    detailsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
                    detailsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                    // ID
                    var idLabel = new TextBlock
                    {
                        Text = "ID:",
                        Margin = new Thickness(0, 0, 10, 10),
                        FontWeight = FontWeights.Bold
                    };
                    Grid.SetRow(idLabel, 0);
                    Grid.SetColumn(idLabel, 0);

                    var idValue = new TextBlock
                    {
                        Text = thesis.Id.ToString(),
                        Margin = new Thickness(0, 0, 0, 10)
                    };
                    Grid.SetRow(idValue, 0);
                    Grid.SetColumn(idValue, 1);

                    // Title
                    var titleLabel = new TextBlock
                    {
                        Text = "Title:",
                        Margin = new Thickness(0, 0, 10, 10),
                        FontWeight = FontWeights.Bold
                    };
                    Grid.SetRow(titleLabel, 1);
                    Grid.SetColumn(titleLabel, 0);

                    var titleValue = new TextBlock
                    {
                        Text = thesis.Titre,
                        Margin = new Thickness(0, 0, 0, 10),
                        TextWrapping = TextWrapping.Wrap
                    };
                    Grid.SetRow(titleValue, 1);
                    Grid.SetColumn(titleValue, 1);

                    // Author
                    var authorLabel = new TextBlock
                    {
                        Text = "Author:",
                        Margin = new Thickness(0, 0, 10, 10),
                        FontWeight = FontWeights.Bold
                    };
                    Grid.SetRow(authorLabel, 2);
                    Grid.SetColumn(authorLabel, 0);

                    var authorValue = new TextBlock
                    {
                        Text = thesis.Auteur,
                        Margin = new Thickness(0, 0, 0, 10)
                    };
                    Grid.SetRow(authorValue, 2);
                    Grid.SetColumn(authorValue, 1);

                    // Type
                    var typeLabel = new TextBlock
                    {
                        Text = "Type:",
                        Margin = new Thickness(0, 0, 10, 10),
                        FontWeight = FontWeights.Bold
                    };
                    Grid.SetRow(typeLabel, 3);
                    Grid.SetColumn(typeLabel, 0);

                    var typeValue = new TextBlock
                    {
                        Text = thesis.Type,
                        Margin = new Thickness(0, 0, 0, 10)
                    };
                    Grid.SetRow(typeValue, 3);
                    Grid.SetColumn(typeValue, 1);

                    // Date
                    var dateLabel = new TextBlock
                    {
                        Text = "Date:",
                        Margin = new Thickness(0, 0, 10, 10),
                        FontWeight = FontWeights.Bold
                    };
                    Grid.SetRow(dateLabel, 4);
                    Grid.SetColumn(dateLabel, 0);

                    var dateValue = new TextBlock
                    {
                        Text = thesis.DatePublication.ToString("dd/MM/yyyy"),
                        Margin = new Thickness(0, 0, 0, 10)
                    };
                    Grid.SetRow(dateValue, 4);
                    Grid.SetColumn(dateValue, 1);

                    // Description
                    var descriptionLabel = new TextBlock
                    {
                        Text = "Description:",
                        Margin = new Thickness(0, 0, 10, 10),
                        FontWeight = FontWeights.Bold
                    };
                    Grid.SetRow(descriptionLabel, 5);
                    Grid.SetColumn(descriptionLabel, 0);

                    var descriptionValue = new TextBlock
                    {
                        Text = thesis.Resume,
                        Margin = new Thickness(0, 0, 0, 10),
                        TextWrapping = TextWrapping.Wrap
                    };
                    Grid.SetRow(descriptionValue, 5);
                    Grid.SetColumn(descriptionValue, 1);

                    detailsGrid.Children.Add(idLabel);
                    detailsGrid.Children.Add(idValue);
                    detailsGrid.Children.Add(titleLabel);
                    detailsGrid.Children.Add(titleValue);
                    detailsGrid.Children.Add(authorLabel);
                    detailsGrid.Children.Add(authorValue);
                    detailsGrid.Children.Add(typeLabel);
                    detailsGrid.Children.Add(typeValue);
                    detailsGrid.Children.Add(dateLabel);
                    detailsGrid.Children.Add(dateValue);
                    detailsGrid.Children.Add(descriptionLabel);
                    detailsGrid.Children.Add(descriptionValue);

                    Grid.SetRow(detailsGrid, 1);

                    var closeButton = new Button
                    {
                        Content = "Close",
                        Padding = new Thickness(20, 10, 20, 10),
                        Margin = new Thickness(20),
                        HorizontalAlignment = HorizontalAlignment.Right
                    };
                    Grid.SetRow(closeButton, 2);

                    closeButton.Click += (s, args) => detailsWindow.Close();

                    grid.Children.Add(titleTextBlock);
                    grid.Children.Add(detailsGrid);
                    grid.Children.Add(closeButton);

                    detailsWindow.Content = grid;
                    detailsWindow.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage("Error viewing thesis details", ex);
            }
        }

        /// <summary>
        /// معالج حدث النقر على زر عرض ملف PDF
        /// </summary>
        private void ViewPDF_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Check if user is logged in to view the PDF
                if (!Session.IsLoggedIn)
                {
                    MessageBox.Show("You must be logged in to view thesis documents", 
                                  "Authentication Required", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                
                if (sender is Button button && button.Tag is Theses thesis)
                {
                    // Log the access attempt for audit purposes
                    LogPdfAccess(thesis.Id, Session.CurrentUserId);
                    
                    string pdfUrl = thesis.FichierUrl;

                    if (string.IsNullOrEmpty(pdfUrl))
                    {
                        MessageBox.Show("PDF file not available for this thesis!", "Information",
                                      MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }

                    try
                    {
                        // التحقق مما إذا كان المسار محلياً أو URL
                        if (pdfUrl.StartsWith("http://") || pdfUrl.StartsWith("https://"))
                        {
                            // فتح URL في المتصفح
                            Process.Start(new ProcessStartInfo
                            {
                                FileName = pdfUrl,
                                UseShellExecute = true
                            });
                        }
                        else
                        {
                            // فتح ملف محلي
                            if (File.Exists(pdfUrl))
                            {
                                Process.Start(new ProcessStartInfo
                                {
                                    FileName = pdfUrl,
                                    UseShellExecute = true
                                });
                            }
                            else
                            {
                                MessageBox.Show("PDF file not found at the specified location!", "Error",
                                              MessageBoxButton.OK, MessageBoxImage.Error);
                                
                                // Report the missing file issue
                                ReportMissingFile(thesis.Id, pdfUrl);
                            }
                        }
                    }
                    catch (System.ComponentModel.Win32Exception winEx)
                    {
                        MessageBox.Show($"Cannot open the PDF file. No application is associated with this file type.\n\nError: {winEx.Message}", 
                                      "Application Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    catch (Exception ex)
                    {
                        ShowErrorMessage("Error processing PDF file", ex);
                    }
                }
                else
                {
                    MessageBox.Show("Please select a thesis first", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage("Error handling PDF request", ex);
            }
        }
        
        /// <summary>
        /// Log PDF access attempts for audit purposes
        /// </summary>
        private void LogPdfAccess(int thesisId, int userId)
        {
            try
            {
                using (var connection = DatabaseConnection.GetConnection())
                {
                    // Check if connection is valid
                    if (connection == null || connection.State != ConnectionState.Open)
                    {
                        // Log can be silently failed if connection isn't available
                        return;
                    }
                    
                    string logQuery = "INSERT INTO access_logs (user_id, these_id, action_type, date_action) " +
                                    "VALUES (@UserId, @ThesisId, 'VIEW_PDF', NOW())";
                    
                    using (MySqlCommand cmd = new MySqlCommand(logQuery, connection))
                    {
                        cmd.Parameters.AddWithValue("@UserId", userId);
                        cmd.Parameters.AddWithValue("@ThesisId", thesisId);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception)
            {
                // Silent logging - don't interrupt user experience for logging errors
            }
        }
        
        /// <summary>
        /// Report missing file issues for administrative review
        /// </summary>
        private void ReportMissingFile(int thesisId, string filePath)
        {
            try
            {
                using (var connection = DatabaseConnection.GetConnection())
                {
                    // Check if connection is valid
                    if (connection == null || connection.State != ConnectionState.Open)
                    {
                        // Report can be silently failed if connection isn't available
                        return;
                    }
                    
                    string reportQuery = "INSERT INTO error_reports (user_id, these_id, error_type, error_details, date_report) " +
                                      "VALUES (@UserId, @ThesisId, 'MISSING_FILE', @FilePath, NOW())";
                    
                    using (MySqlCommand cmd = new MySqlCommand(reportQuery, connection))
                    {
                        cmd.Parameters.AddWithValue("@UserId", Session.CurrentUserId);
                        cmd.Parameters.AddWithValue("@ThesisId", thesisId);
                        cmd.Parameters.AddWithValue("@FilePath", filePath);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception)
            {
                // Silent reporting - don't interrupt user experience for reporting errors
            }
        }

        /// <summary>
        /// معالج حدث النقر على زر إرسال رسالة
        /// </summary>
        private void SendMessage_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // First check if user is logged in
                if (!Session.IsLoggedIn)
                {
                    MessageBox.Show("You must be logged in to send messages", 
                                  "Authentication Required", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                
                // Use the session user ID instead of the stored one for better security
                int userId = Session.CurrentUserId;
                string userName = Session.CurrentUserName;
                
                if (sender is Button button && button.Tag is Theses thesis)
                {
                    // إنشاء نافذة حوار لإرسال رسالة
                    var messageWindow = new Window
                    {
                        Title = $"Send Message about: {thesis.Titre}",
                        Width = 400,
                        Height = 350,
                        WindowStartupLocation = WindowStartupLocation.CenterScreen,
                        ResizeMode = ResizeMode.NoResize
                    };

                    var grid = new Grid();
                    grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
                    grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
                    grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                    grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });

                    // Add information about the current user
                    var userInfoTextBlock = new TextBlock
                    {
                        Text = $"Sending as: {userName}",
                        Margin = new Thickness(10, 10, 10, 5),
                        FontSize = 12,
                        FontStyle = FontStyles.Italic
                    };
                    Grid.SetRow(userInfoTextBlock, 0);

                    var titleTextBlock = new TextBlock
                    {
                        Text = "Enter your message:",
                        Margin = new Thickness(10, 5, 10, 5),
                        FontSize = 14,
                        FontWeight = FontWeights.Bold
                    };
                    Grid.SetRow(titleTextBlock, 1);

                    var messageTextBox = new TextBox
                    {
                        Margin = new Thickness(10),
                        TextWrapping = TextWrapping.Wrap,
                        AcceptsReturn = true,
                        MinHeight = 150,
                        VerticalScrollBarVisibility = ScrollBarVisibility.Auto
                    };
                    Grid.SetRow(messageTextBox, 2);

                    var buttonPanel = new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        HorizontalAlignment = HorizontalAlignment.Right,
                        Margin = new Thickness(10)
                    };
                    Grid.SetRow(buttonPanel, 3);

                    var sendButton = new Button
                    {
                        Content = "Send",
                        Padding = new Thickness(15, 5, 15, 5),
                        Margin = new Thickness(0, 0, 10, 0),
                        Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#008080")),
                        Foreground = Brushes.White
                    };

                    var cancelButton = new Button
                    {
                        Content = "Cancel",
                        Padding = new Thickness(15, 5, 15, 5)
                    };

                    buttonPanel.Children.Add(sendButton);
                    buttonPanel.Children.Add(cancelButton);

                    grid.Children.Add(userInfoTextBlock);
                    grid.Children.Add(titleTextBlock);
                    grid.Children.Add(messageTextBox);
                    grid.Children.Add(buttonPanel);

                    messageWindow.Content = grid;

                    // معالجة أحداث الأزرار
                    sendButton.Click += (s, args) =>
                    {
                        try
                        {
                            string message = messageTextBox.Text.Trim();

                            if (string.IsNullOrEmpty(message))
                            {
                                MessageBox.Show("Please enter a message!", "Validation Error",
                                              MessageBoxButton.OK, MessageBoxImage.Warning);
                                return;
                            }
                            
                            // Revalidate session before sending
                            if (!Session.IsLoggedIn)
                            {
                                MessageBox.Show("Your session has expired. Please log in again.", 
                                              "Session Error", MessageBoxButton.OK, MessageBoxImage.Error);
                                messageWindow.Close();
                                return;
                            }

                            try
                            {
                                // Use the centralized database connection management
                                using (var connection = DatabaseConnection.GetConnection())
                                {
                                    // Check if connection is valid
                                    if (connection == null || connection.State != ConnectionState.Open)
                                    {
                                        MessageBox.Show("Database connection error. Please try again.", "Connection Error",
                                            MessageBoxButton.OK, MessageBoxImage.Error);
                                        return;
                                    }

                                    // Save the message to the database
                                    string insertQuery = "INSERT INTO contacts (user_id, these_id, message, date_envoi) " +
                                                       "VALUES (@UserId, @ThesisId, @Message, NOW())";

                                    using (MySqlCommand cmd = new MySqlCommand(insertQuery, connection))
                                    {
                                        cmd.Parameters.AddWithValue("@UserId", userId);
                                        cmd.Parameters.AddWithValue("@ThesisId", thesis.Id);
                                        cmd.Parameters.AddWithValue("@Message", message);

                                        cmd.ExecuteNonQuery();
                                    }
                                }

                                MessageBox.Show("Message sent successfully!", "Success",
                                              MessageBoxButton.OK, MessageBoxImage.Information);

                                messageWindow.Close();
                            }
                            catch (MySqlException ex)
                            {
                                // The connection is already properly disposed by the using statement
                                HandleDatabaseError(ex);
                            }
                        }
                        catch (Exception ex)
                        {
                            ShowErrorMessage("Error sending message", ex);
                        }
                    };

                    cancelButton.Click += (s, args) => messageWindow.Close();

                    messageWindow.ShowDialog();
                }
                else
                {
                    MessageBox.Show("Please select a thesis first", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage("Error opening message window", ex);
            }
        }

        /// <summary>
        /// معالج حدث النقر على زر الحذف من المفضلة
        /// </summary>
        private void RemoveFromFavorites_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // First check if user is logged in
                if (!Session.IsLoggedIn)
                {
                    MessageBox.Show("You must be logged in to manage favorites", 
                                  "Authentication Required", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                
                // Use the session user ID instead of the stored one for better security
                int userId = Session.CurrentUserId;
                
                if (sender is Button button && button.Tag is Theses thesis)
                {
                    // التأكيد قبل الحذف
                    MessageBoxResult result = MessageBox.Show(
                        "Are you sure you want to remove this thesis from your favorites?",
                        "Confirmation",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (result == MessageBoxResult.No)
                    {
                        return;
                    }

                    try
                    {
                        // Use the centralized database connection management
                        using (var connection = DatabaseConnection.GetConnection())
                        {
                            // Check if connection is valid
                            if (connection == null || connection.State != ConnectionState.Open)
                            {
                                MessageBox.Show("Database connection error. Please try again later.", "Connection Error", 
                                    MessageBoxButton.OK, MessageBoxImage.Error);
                                return;
                            }

                            // Verify the record exists and belongs to this user
                            string verifyQuery = "SELECT COUNT(*) FROM favoris WHERE id = @FavorisId AND user_id = @UserId";
                            using (MySqlCommand verifyCmd = new MySqlCommand(verifyQuery, connection))
                            {
                                verifyCmd.Parameters.AddWithValue("@FavorisId", thesis.FavorisId);
                                verifyCmd.Parameters.AddWithValue("@UserId", userId);

                                int count = Convert.ToInt32(verifyCmd.ExecuteScalar());
                                if (count == 0)
                                {
                                    MessageBox.Show("This favorite entry doesn't exist or doesn't belong to your account", 
                                                "Permission Denied", MessageBoxButton.OK, MessageBoxImage.Warning);
                                    return;
                                }
                            }

                            // Delete the thesis from favorites
                            string deleteQuery = "DELETE FROM favoris WHERE id = @FavorisId AND user_id = @UserId";

                            using (MySqlCommand cmd = new MySqlCommand(deleteQuery, connection))
                            {
                                cmd.Parameters.AddWithValue("@FavorisId", thesis.FavorisId);
                                cmd.Parameters.AddWithValue("@UserId", userId);

                                int rowsAffected = cmd.ExecuteNonQuery();

                                if (rowsAffected > 0)
                                {
                                    MessageBox.Show("Thesis removed from favorites successfully!", "Success",
                                                MessageBoxButton.OK, MessageBoxImage.Information);

                                    // Reload data after deletion
                                    LoadFavorites();
                                }
                                else
                                {
                                    MessageBox.Show("Failed to remove thesis from favorites!", "Error",
                                                MessageBoxButton.OK, MessageBoxImage.Error);
                                }
                            }
                        }
                    }
                    catch (MySqlException ex)
                    {
                        HandleDatabaseError(ex);
                    }
                    catch (Exception ex)
                    {
                        ShowErrorMessage("Error removing thesis from favorites", ex);
                    }
                }
                else
                {
                    MessageBox.Show("Please select a thesis first", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage("Error processing remove from favorites request", ex);
            }
        }

        /// <summary>
        /// عرض رسالة خطأ مع تفاصيل الاستثناء
        /// </summary>
        private void ShowErrorMessage(string message, Exception ex)
        {
            MessageBox.Show($"{message}: {ex.Message}\n\nStack Trace: {ex.StackTrace}",
                          "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        /// <summary>
        /// Since we're using DatabaseConnection.GetConnection(), we don't need to manually clean up connections
        /// All connections are properly managed through using statements for automatic disposal
        /// </summary>
        ~FavoritesView()
        {
            // No cleanup needed for database connections as they're now managed by DatabaseConnection class
            // and disposed automatically by using statements
            System.Diagnostics.Debug.WriteLine("FavoritesView is being finalized");
        }
    }
}
