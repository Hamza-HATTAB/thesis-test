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
using System.Runtime.Versioning;
using System.Windows.Markup;
#if WINDOWS
using MahApps.Metro.IconPacks;
#endif
using DataGridNamespace.Controls;
using DataGridNamespace.Utilities;
using System.ComponentModel;

namespace DataGridNamespace.Admin
{
    /// <summary>
    /// صفحة الأطروحات مع واجهة مستخدم حديثة وميزات متقدمة
    /// </summary>
    public partial class ThesisView : UserControl
    {
        #region Properties
        private int _currentPage = 1;
        private int _itemsPerPage = 20;
        private int _totalItems = 0;
        private string _currentSearchText = "";
        private string _currentThesisType = "All";
        private int _currentUserId;
        private MySqlConnection _connection;

        public int CurrentPage
        {
            get => _currentPage;
            set
            {
                _currentPage = value;
                UpdatePaginationControls();
            }
        }

        public int ItemsPerPage
        {
            get => _itemsPerPage;
            set
            {
                _itemsPerPage = value;
                UpdatePaginationControls();
            }
        }

        public int TotalItems
        {
            get => _totalItems;
            set
            {
                _totalItems = value;
                UpdatePaginationControls();
            }
        }

        public string CurrentSearchText
        {
            get => _currentSearchText;
            set
            {
                _currentSearchText = value;
                CurrentPage = 1;
                LoadThesesData();
            }
        }

        public string CurrentThesisType
        {
            get => _currentThesisType;
            set
            {
                _currentThesisType = value;
                CurrentPage = 1;
                LoadThesesData();
            }
        }

        public int CurrentUserId
        {
            get => _currentUserId;
            set
            {
                _currentUserId = value;
            }
        }
        #endregion

        #region Initialization
        /// <summary>
        /// إنشاء مثيل جديد من صفحة الأطروحات
        /// </summary>
        public ThesisView()
        {
            try
            {
                InitializeComponent();
                Initialize();
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError("Error initializing ThesisView", ex, showMessageBox: true);
            }
        }

        /// <summary>
        /// Initialize the control after it's fully loaded
        /// </summary>
        private void Initialize()
        {
            if (!DesignerProperties.GetIsInDesignMode(this))
            {
                CurrentUserId = Session.IsLoggedIn ? Session.CurrentUserId : 1;
                InitializeDatabaseConnection();
                LoadThesesData();
            }
        }
        #endregion

        #region Database Methods
        /// <summary>
        /// تهيئة الاتصال بقاعدة البيانات
        /// </summary>
        private void InitializeDatabaseConnection()
        {
            try
            {
                _connection = DatabaseConnection.GetConnection();
                if (_connection.State != ConnectionState.Open)
                {
                    throw new Exception("Could not open database connection");
                }
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError("Database connection error", ex, showMessageBox: true);
            }
        }

        /// <summary>
        /// تحميل الأطروحات من قاعدة البيانات مع دعم البحث والتصفية والترقيم الصفحي
        /// </summary>
        private void LoadThesesData()
        {
            try
            {
                using (var connection = DatabaseConnection.GetConnection())
                {
                    if (connection == null)
                    {
                        ErrorLogger.LogError("Failed to establish database connection", showMessageBox: true);
                        return;
                    }

                    string countQuery = "SELECT COUNT(*) FROM theses t " +
                                       "JOIN users u ON t.user_id = u.Id";

                    string query = "SELECT t.*, u.Nom AS Auteur FROM theses t " +
                                  "JOIN users u ON t.user_id = u.Id";

                    if (CurrentThesisType != "All")
                    {
                        countQuery += " WHERE t.Type = @Type";
                        query += " WHERE t.Type = @Type";
                    }

                    if (!string.IsNullOrEmpty(CurrentSearchText))
                    {
                        string searchCondition = " (t.Titre LIKE @SearchText OR u.Nom LIKE @SearchText)";
                        
                        if (CurrentThesisType != "All")
                        {
                            countQuery += " AND " + searchCondition;
                            query += " AND " + searchCondition;
                        }
                        else
                        {
                            countQuery += " WHERE " + searchCondition;
                            query += " WHERE " + searchCondition;
                        }
                    }

                    query += " ORDER BY t.Id DESC LIMIT @Offset, @Limit";

                    using (MySqlCommand countCmd = new MySqlCommand(countQuery, connection))
                    {
                        if (CurrentThesisType != "All")
                        {
                            countCmd.Parameters.AddWithValue("@Type", CurrentThesisType);
                        }
                        
                        if (!string.IsNullOrEmpty(CurrentSearchText))
                        {
                            countCmd.Parameters.AddWithValue("@SearchText", "%" + CurrentSearchText + "%");
                        }
                        
                        TotalItems = Convert.ToInt32(countCmd.ExecuteScalar());
                    }

                    using (MySqlCommand cmd = new MySqlCommand(query, connection))
                    {
                        if (CurrentThesisType != "All")
                        {
                            cmd.Parameters.AddWithValue("@Type", CurrentThesisType);
                        }
                        
                        if (!string.IsNullOrEmpty(CurrentSearchText))
                        {
                            cmd.Parameters.AddWithValue("@SearchText", "%" + CurrentSearchText + "%");
                        }
                        
                        cmd.Parameters.AddWithValue("@Offset", (CurrentPage - 1) * ItemsPerPage);
                        cmd.Parameters.AddWithValue("@Limit", ItemsPerPage);
                        
                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            List<Theses> thesesList = new List<Theses>();
                            
                            while (reader.Read())
                            {
                                var thesis = new Theses
                                {
                                    Id = reader.GetInt32("Id"),
                                    Titre = reader.GetString("Titre"),
                                    Resume = reader.GetString("Resume"),
                                    Type = reader.GetString("Type"),
                                    DatePublication = reader.GetDateTime("DatePublication"),
                                    FichierUrl = reader.GetString("FichierUrl"),
                                    UserId = reader.GetInt32("user_id"),
                                    Auteur = reader.GetString("Auteur")
                                };
                                
                                thesesList.Add(thesis);
                            }
                            
                            if (ThesesDataGrid != null)
                            {
                                ThesesDataGrid.ItemsSource = thesesList;
                            }
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
                ErrorLogger.LogError("Error loading theses", ex, showMessageBox: true);
            }
        }
        #endregion

        #region UI Methods
        /// <summary>
        /// تحديث معلومات الترقيم الصفحي
        /// </summary>
        private void UpdatePaginationControls()
        {
            try
            {
                int totalPages = (int)Math.Ceiling(TotalItems / (double)ItemsPerPage);
                if (totalPages == 0) totalPages = 1;
                
                // Update the data context for the page info binding
                var pageInfo = new { CurrentPage, TotalPages = totalPages };
                this.DataContext = pageInfo;
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError("Error updating pagination info", ex);
            }
        }
        #endregion

        #region Event Handlers
        /// <summary>
        /// معالج حدث تغيير نص البحث
        /// </summary>
        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (SearchTextBox != null)
                {
                    CurrentSearchText = SearchTextBox.Text;
                }
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError("Error in search text changed", ex);
            }
        }

        /// <summary>
        /// معالج حدث تغيير نوع الأطروحة
        /// </summary>
        private void TypeFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (TypeFilterComboBox != null)
                {
                    CurrentThesisType = TypeFilterComboBox.SelectedItem?.ToString() ?? "All";
                }
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError("Error in type filter changed", ex);
            }
        }

        /// <summary>
        /// معالج حدث النقر على زر الصفحة السابقة
        /// </summary>
        private void OnPreviousPage_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (CurrentPage > 1)
                {
                    CurrentPage--;
                }
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError("Error in previous page click", ex);
            }
        }

        /// <summary>
        /// معالج حدث النقر على زر الصفحة التالية
        /// </summary>
        private void OnNextPage_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (CurrentPage * ItemsPerPage < TotalItems)
                {
                    CurrentPage++;
                }
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError("Error in next page click", ex);
            }
        }

        /// <summary>
        /// معالج حدث النقر على زر التحديث
        /// </summary>
        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                CurrentPage = 1;
                LoadThesesData();
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError("Error in refresh click", ex);
            }
        }

        /// <summary>
        /// معالج حدث النقر على زر إضافة أطروحة
        /// </summary>
        private void AddThesis_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var addThesisWindow = new Window
                {
                    Title = "Add New Thesis",
                    Width = 600,
                    Height = 700,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    ResizeMode = ResizeMode.NoResize
                };

                var grid = new Grid();
                grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
                grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });

                var titleTextBlock = new TextBlock
                {
                    Text = "Add New Thesis",
                    FontSize = 24,
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(20, 20, 20, 20),
                    HorizontalAlignment = HorizontalAlignment.Center
                };

                var formGrid = new Grid();
                formGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
                formGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
                formGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
                formGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
                formGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
                formGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
                formGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) });

                // Title
                var titleLabel = new TextBlock
                {
                    Text = "Title:",
                    Margin = new Thickness(0, 0, 10, 10),
                    FontWeight = FontWeights.Bold,
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetRow(titleLabel, 0);
                Grid.SetColumn(titleLabel, 0);

                var titleTextBox = new TextBox
                {
                    Margin = new Thickness(0, 0, 0, 10),
                    Padding = new Thickness(5),
                    Height = 30
                };
                Grid.SetRow(titleTextBox, 0);
                Grid.SetColumn(titleTextBox, 1);

                // Type
                var typeLabel = new TextBlock
                {
                    Text = "Type:",
                    Margin = new Thickness(0, 0, 10, 10),
                    FontWeight = FontWeights.Bold,
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetRow(typeLabel, 1);
                Grid.SetColumn(typeLabel, 0);

                var typeComboBox = new ComboBox
                {
                    Margin = new Thickness(0, 0, 0, 10),
                    Height = 30
                };
                typeComboBox.Items.Add("Master");
                typeComboBox.Items.Add("Doctorate");
                typeComboBox.SelectedIndex = 0;
                Grid.SetRow(typeComboBox, 1);
                Grid.SetColumn(typeComboBox, 1);

                // File URL
                var fileUrlLabel = new TextBlock
                {
                    Text = "File URL:",
                    Margin = new Thickness(0, 0, 10, 10),
                    FontWeight = FontWeights.Bold,
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetRow(fileUrlLabel, 2);
                Grid.SetColumn(fileUrlLabel, 0);

                var fileUrlTextBox = new TextBox
                {
                    Margin = new Thickness(0, 0, 0, 10),
                    Padding = new Thickness(5),
                    Height = 30
                };
                Grid.SetRow(fileUrlTextBox, 2);
                Grid.SetColumn(fileUrlTextBox, 1);

                // Date
                var dateLabel = new TextBlock
                {
                    Text = "Date:",
                    Margin = new Thickness(0, 0, 10, 10),
                    FontWeight = FontWeights.Bold,
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetRow(dateLabel, 3);
                Grid.SetColumn(dateLabel, 0);

                var datePicker = new DatePicker
                {
                    Margin = new Thickness(0, 0, 0, 10),
                    SelectedDate = DateTime.Now,
                    Height = 30
                };
                Grid.SetRow(datePicker, 3);
                Grid.SetColumn(datePicker, 1);

                // Description
                var descriptionLabel = new TextBlock
                {
                    Text = "Description:",
                    Margin = new Thickness(0, 0, 10, 10),
                    FontWeight = FontWeights.Bold,
                    VerticalAlignment = VerticalAlignment.Top
                };
                Grid.SetRow(descriptionLabel, 4);
                Grid.SetColumn(descriptionLabel, 0);

                var descriptionTextBox = new TextBox
                {
                    Margin = new Thickness(0, 0, 0, 10),
                    Padding = new Thickness(5),
                    TextWrapping = TextWrapping.Wrap,
                    AcceptsReturn = true,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    Height = 150
                };
                Grid.SetRow(descriptionTextBox, 4);
                Grid.SetColumn(descriptionTextBox, 1);

                formGrid.Children.Add(titleLabel);
                formGrid.Children.Add(titleTextBox);
                formGrid.Children.Add(typeLabel);
                formGrid.Children.Add(typeComboBox);
                formGrid.Children.Add(fileUrlLabel);
                formGrid.Children.Add(fileUrlTextBox);
                formGrid.Children.Add(dateLabel);
                formGrid.Children.Add(datePicker);
                formGrid.Children.Add(descriptionLabel);
                formGrid.Children.Add(descriptionTextBox);

                Grid.SetRow(formGrid, 1);

                var buttonPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Margin = new Thickness(20)
                };
                Grid.SetRow(buttonPanel, 2);

                var saveButton = new Button
                {
                    Content = "Save",
                    Padding = new Thickness(20, 10, 20, 10),
                    Margin = new Thickness(0, 0, 10, 0),
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#008080")),
                    Foreground = Brushes.White
                };

                var cancelButton = new Button
                {
                    Content = "Cancel",
                    Padding = new Thickness(20, 10, 20, 10)
                };

                buttonPanel.Children.Add(saveButton);
                buttonPanel.Children.Add(cancelButton);

                grid.Children.Add(titleTextBlock);
                grid.Children.Add(formGrid);
                grid.Children.Add(buttonPanel);

                addThesisWindow.Content = grid;

                // معالجة أحداث الأزرار
                saveButton.Click += (s, args) =>
                {
                    try
                    {
                        if (string.IsNullOrEmpty(titleTextBox.Text))
                        {
                            MessageBox.Show("Please enter a title!", "Validation Error", 
                                          MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }
                        
                        if (string.IsNullOrEmpty(descriptionTextBox.Text))
                        {
                            MessageBox.Show("Please enter a description!", "Validation Error", 
                                          MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }
                        
                        if (string.IsNullOrEmpty(fileUrlTextBox.Text))
                        {
                            MessageBox.Show("Please enter a file URL!", "Validation Error", 
                                          MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }
                        
                        if (datePicker.SelectedDate == null)
                        {
                            MessageBox.Show("Please select a date!", "Validation Error", 
                                          MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }

                        using (var connection = DatabaseConnection.GetConnection())
                        {
                            if (connection == null)
                            {
                                ErrorLogger.LogError("Database connection error", showMessageBox: true);
                                return;
                            }

                            string insertQuery = "INSERT INTO theses (Titre, Description, Type, DatePublication, FichierUrl, user_id) " +
                                               "VALUES (@Title, @Description, @Type, @Date, @FileUrl, @UserId)";

                            using (MySqlCommand cmd = new MySqlCommand(insertQuery, connection))
                            {
                                cmd.Parameters.AddWithValue("@Title", titleTextBox.Text);
                                cmd.Parameters.AddWithValue("@Description", descriptionTextBox.Text);
                                cmd.Parameters.AddWithValue("@Type", typeComboBox.SelectedItem.ToString());
                                cmd.Parameters.AddWithValue("@Date", datePicker.SelectedDate.Value);
                                cmd.Parameters.AddWithValue("@FileUrl", fileUrlTextBox.Text);
                                cmd.Parameters.AddWithValue("@UserId", CurrentUserId);
                                cmd.ExecuteNonQuery();
                            }
                        }

                        MessageBox.Show("Thesis added successfully!", "Success", 
                                      MessageBoxButton.OK, MessageBoxImage.Information);
                        LoadThesesData();
                        addThesisWindow.Close();
                    }
                    catch (MySqlException ex)
                    {
                        HandleDatabaseError(ex);
                    }
                    catch (Exception ex)
                    {
                        ErrorLogger.LogError("Error adding thesis", ex, showMessageBox: true);
                    }
                };

                cancelButton.Click += (s, args) => addThesisWindow.Close();

                addThesisWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError("Error opening add thesis window", ex, showMessageBox: true);
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
                        Text = $"Thesis Details - {thesis.Titre}",
                        FontSize = 24,
                        FontWeight = FontWeights.Bold,
                        Margin = new Thickness(20, 20, 20, 20),
                        HorizontalAlignment = HorizontalAlignment.Center
                    };

                    var detailsGrid = new Grid();
                    detailsGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
                    detailsGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
                    detailsGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
                    detailsGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
                    detailsGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
                    detailsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
                    detailsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) });

                    // Title
                    var titleLabel = new TextBlock
                    {
                        Text = "Title:",
                        Margin = new Thickness(0, 0, 10, 10),
                        FontWeight = FontWeights.Bold,
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    Grid.SetRow(titleLabel, 0);
                    Grid.SetColumn(titleLabel, 0);

                    var titleValue = new TextBlock
                    {
                        Text = thesis.Titre,
                        Margin = new Thickness(0, 0, 0, 10),
                        TextWrapping = TextWrapping.Wrap
                    };
                    Grid.SetRow(titleValue, 0);
                    Grid.SetColumn(titleValue, 1);

                    // Type
                    var typeLabel = new TextBlock
                    {
                        Text = "Type:",
                        Margin = new Thickness(0, 0, 10, 10),
                        FontWeight = FontWeights.Bold,
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    Grid.SetRow(typeLabel, 1);
                    Grid.SetColumn(typeLabel, 0);

                    var typeValue = new TextBlock
                    {
                        Text = thesis.Type,
                        Margin = new Thickness(0, 0, 0, 10)
                    };
                    Grid.SetRow(typeValue, 1);
                    Grid.SetColumn(typeValue, 1);

                    // Date
                    var dateLabel = new TextBlock
                    {
                        Text = "Date:",
                        Margin = new Thickness(0, 0, 10, 10),
                        FontWeight = FontWeights.Bold,
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    Grid.SetRow(dateLabel, 2);
                    Grid.SetColumn(dateLabel, 0);

                    var dateValue = new TextBlock
                    {
                        Text = thesis.DatePublication.ToString("dd/MM/yyyy"),
                        Margin = new Thickness(0, 0, 0, 10)
                    };
                    Grid.SetRow(dateValue, 2);
                    Grid.SetColumn(dateValue, 1);

                    // Author
                    var authorLabel = new TextBlock
                    {
                        Text = "Author:",
                        Margin = new Thickness(0, 0, 10, 10),
                        FontWeight = FontWeights.Bold,
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    Grid.SetRow(authorLabel, 3);
                    Grid.SetColumn(authorLabel, 0);

                    var authorValue = new TextBlock
                    {
                        Text = thesis.Auteur,
                        Margin = new Thickness(0, 0, 0, 10)
                    };
                    Grid.SetRow(authorValue, 3);
                    Grid.SetColumn(authorValue, 1);

                    // Description
                    var descriptionLabel = new TextBlock
                    {
                        Text = "Description:",
                        Margin = new Thickness(0, 0, 10, 10),
                        FontWeight = FontWeights.Bold,
                        VerticalAlignment = VerticalAlignment.Top
                    };
                    Grid.SetRow(descriptionLabel, 4);
                    Grid.SetColumn(descriptionLabel, 0);

                    var descriptionValue = new TextBlock
                    {
                        Text = thesis.Resume,
                        Margin = new Thickness(0, 0, 0, 10),
                        TextWrapping = TextWrapping.Wrap,
                        TextAlignment = TextAlignment.Justify
                    };
                    Grid.SetRow(descriptionValue, 4);
                    Grid.SetColumn(descriptionValue, 1);

                    detailsGrid.Children.Add(titleLabel);
                    detailsGrid.Children.Add(titleValue);
                    detailsGrid.Children.Add(typeLabel);
                    detailsGrid.Children.Add(typeValue);
                    detailsGrid.Children.Add(dateLabel);
                    detailsGrid.Children.Add(dateValue);
                    detailsGrid.Children.Add(authorLabel);
                    detailsGrid.Children.Add(authorValue);
                    detailsGrid.Children.Add(descriptionLabel);
                    detailsGrid.Children.Add(descriptionValue);

                    Grid.SetRow(detailsGrid, 1);

                    var closeButton = new Button
                    {
                        Content = "Close",
                        Padding = new Thickness(20, 10, 20, 10),
                        HorizontalAlignment = HorizontalAlignment.Right,
                        Margin = new Thickness(20)
                    };
                    Grid.SetRow(closeButton, 2);

                    grid.Children.Add(titleTextBlock);
                    grid.Children.Add(detailsGrid);
                    grid.Children.Add(closeButton);

                    detailsWindow.Content = grid;
                    closeButton.Click += (s, args) => detailsWindow.Close();
                    detailsWindow.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError("Error viewing thesis details", ex, showMessageBox: true);
            }
        }

        /// <summary>
        /// معالج حدث النقر على زر عرض ملف PDF
        /// </summary>
        private void ViewPDF_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button button && button.Tag is Theses thesis)
                {
                    if (!string.IsNullOrEmpty(thesis.FichierUrl))
                    {
                        Process.Start(thesis.FichierUrl);
                    }
                    else
                    {
                        MessageBox.Show("No PDF file available for this thesis.", "Information", 
                                      MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError("Error viewing PDF", ex, showMessageBox: true);
            }
        }

        /// <summary>
        /// معالج حدث النقر على زر إرسال رسالة
        /// </summary>
        private void SendMessage_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button button && button.Tag is Theses thesis)
                {
                    // TODO: Implement message sending functionality
                    MessageBox.Show("Message sending functionality will be implemented soon.", "Coming Soon", 
                                  MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError("Error sending message", ex, showMessageBox: true);
            }
        }

        /// <summary>
        /// معالج حدث النقر على زر إضافة إلى المفضلة
        /// </summary>
        private void OnAddToFavorites(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button button && button.Tag is Theses thesis)
                {
                    using (var connection = DatabaseConnection.GetConnection())
                    {
                        if (connection == null)
                        {
                            ErrorLogger.LogError("Database connection error", showMessageBox: true);
                            return;
                        }

                        string insertQuery = "INSERT INTO favorites (thesis_id, user_id) " +
                                           "VALUES (@ThesisId, @UserId) " +
                                           "ON DUPLICATE KEY UPDATE created_at = NOW()";

                        using (MySqlCommand cmd = new MySqlCommand(insertQuery, connection))
                        {
                            cmd.Parameters.AddWithValue("@ThesisId", thesis.Id);
                            cmd.Parameters.AddWithValue("@UserId", CurrentUserId);
                            cmd.ExecuteNonQuery();
                        }

                        MessageBox.Show("Thesis added to favorites!", "Success", 
                                      MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (MySqlException ex)
            {
                HandleDatabaseError(ex);
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError("Error adding to favorites", ex, showMessageBox: true);
            }
        }
        #endregion

        #region Error Handling
        /// <summary>
        /// معالجة أخطاء قاعدة البيانات
        /// </summary>
        private void HandleDatabaseError(MySqlException ex)
        {
            string errorMessage = "Database error occurred: " + ex.Message;
            ErrorLogger.LogError(errorMessage, ex, showMessageBox: true);
        }

        /// <summary>
        /// عرض رسالة خطأ مع تفاصيل الاستثناء
        /// </summary>
        private void HandleError(string message, Exception ex = null)
        {
            string fullMessage = message;
            if (ex != null)
            {
                fullMessage += $"\n\nError Details:\n{ex.Message}";
            }
            MessageBox.Show(fullMessage, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        #endregion
    }
}
