using System;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Collections.Generic; // Required for Dictionary
using System.Windows; // Required for Application.Current for logging dispatch

namespace DataGridNamespace
{
    /// <summary>
    /// Central session management class for user authentication state.
    /// Handles login, logout, session verification, and basic activity logging.
    /// </summary>
    public static class Session
    {
        private static int _currentUserId;
        private static string _currentUsername;
        private static string _currentUserRole;
        private static bool _isLoggedIn;
        private static DateTime _loginTime;
        private static DateTime _lastActivity;
        private static string _sessionToken;
        private static readonly int _sessionTimeoutMinutes = 30; // Default session timeout

        // --- Public Properties ---
        public static int CurrentUserId
        {
            get
            {
                // No need to check IsLoggedIn here, getter should just return value
                // Callers should check IsLoggedIn before accessing if needed.
                return _currentUserId;
            }
            private set { _currentUserId = value; }
        }

        public static string CurrentUsername
        {
            get { return _currentUsername; }
            private set { _currentUsername = value; }
        }

        // Alias for compatibility if needed
        public static string CurrentUserName => CurrentUsername;

        public static string CurrentUserRole
        {
            get { return _currentUserRole; }
            private set { _currentUserRole = value; }
        }

        /// <summary>
        /// Gets a value indicating whether a user is currently logged in and the session is active (not timed out).
        /// </summary>
        public static bool IsLoggedIn => _isLoggedIn && !IsSessionTimedOut();

        public static DateTime LoginTime => _loginTime;

        public static string SessionToken => _sessionToken;

        // --- Public Methods ---

        /// <summary>
        /// Initializes a new user session upon successful login.
        /// This is the primary method to start a session.
        /// </summary>
        /// <param name="userId">The ID of the logged-in user.</param>
        /// <param name="userRole">The role of the logged-in user.</param>
        /// <param name="userName">The username of the logged-in user.</param>
        public static void InitializeSession(int userId, string userRole, string userName)
        {
            if (userId <= 0 || string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(userRole))
            {
                Debug.WriteLine("Error: Attempted to initialize session with invalid parameters.");
                // Optionally throw an ArgumentException here
                return;
            }

            CurrentUserId = userId;
            CurrentUsername = userName;
            CurrentUserRole = userRole;
            _isLoggedIn = true;
            _loginTime = DateTime.Now;
            _lastActivity = DateTime.Now; // Set initial activity time
            _sessionToken = GenerateSessionToken(userId, userName);

            Debug.WriteLine($"Session initialized for User: {userName} (ID: {userId}, Role: {userRole}) at {_loginTime}.");

            LogActivity("login"); // Log the login event
        }

        /// <summary>
        /// Ends the current user session and clears all session data.
        /// </summary>
        public static void Logout()
        {
            if (_isLoggedIn) // Only log if actually logged in
            {
                Debug.WriteLine($"Session ending for User: {CurrentUsername} (ID: {CurrentUserId}) at {DateTime.Now}");
                LogActivity("logout"); // Log before clearing data
            }

            // Clear all session state regardless of previous state
            _currentUserId = 0;
            _currentUsername = null;
            _currentUserRole = null;
            _isLoggedIn = false;
            _sessionToken = null;
            _loginTime = default;
            _lastActivity = default;
        }

        /// <summary>
        /// Clears the current session data. Alias for Logout().
        /// Included for compatibility with code potentially calling ClearSession().
        /// </summary>
        public static void ClearSession()
        {
            Logout();
        }

        /// <summary>
        /// Verifies if the current session is active (logged in and not timed out).
        /// Refreshes the last activity time if the session is valid.
        /// </summary>
        /// <returns>True if the session is valid, otherwise false (session is logged out if timed out).</returns>
        public static bool VerifySession()
        {
            if (!_isLoggedIn) return false; // Not logged in

            if (IsSessionTimedOut())
            {
                Debug.WriteLine($"Session for User ID {CurrentUserId} timed out due to inactivity.");
                Logout(); // Log out the user if timed out
                return false;
            }

            // Session is valid, refresh activity time
            RefreshSession();
            return true;
        }

        /// <summary>
        /// Checks if the session has expired due to inactivity, without logging out.
        /// </summary>
        /// <returns>True if the session is timed out, otherwise false.</returns>
        public static bool IsSessionTimedOut()
        {
            if (!_isLoggedIn) return false; // Can't time out if not logged in

            TimeSpan inactiveDuration = DateTime.Now - _lastActivity;
            bool isTimedOut = inactiveDuration.TotalMinutes > _sessionTimeoutMinutes;
            // Optional: Add debug logging for timeout check
            // if(isTimedOut) Debug.WriteLine($"Session timeout check: UserID {CurrentUserId}, Inactive: {inactiveDuration.TotalMinutes} min");
            return isTimedOut;
        }

        /// <summary>
        /// Updates the last activity timestamp for the current session.
        /// Should be called periodically during user activity to prevent timeout.
        /// </summary>
        public static void RefreshSession()
        {
            if (_isLoggedIn)
            {
                _lastActivity = DateTime.Now;
                // Debug.WriteLine($"Session refreshed for UserID {CurrentUserId} at: {_lastActivity}"); // Can be noisy
            }
        }

        // --- Private Helper Methods ---

        /// <summary>
        /// Generates a URL-safe Base64 encoded SHA256 hash as a session token.
        /// </summary>
        private static string GenerateSessionToken(int userId, string username)
        {
            // Combine unique and time-sensitive elements
            string timeComponent = DateTime.UtcNow.Ticks.ToString("x"); // Hex ticks
            string userComponent = $"{userId}-{username}";
            string randomComponent = Guid.NewGuid().ToString("N"); // Compact GUID format
            string tokenSource = $"{timeComponent}|{userComponent}|{randomComponent}|SOME_SECRET_SALT"; // Add a secret salt

            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = Encoding.UTF8.GetBytes(tokenSource);
                byte[] hash = sha256.ComputeHash(bytes);
                // Convert to URL-safe Base64 string
                return Convert.ToBase64String(hash).Replace('+', '-').Replace('/', '_').TrimEnd('=');
            }
        }

        /// <summary>
        /// Logs user activity (e.g., login, logout) to the 'user_activity_log' table.
        /// Fails silently if database operations encounter issues.
        /// </summary>
        /// <param name="activityType">The type of activity (e.g., "login", "logout").</param>
        private static void LogActivity(string activityType)
        {
            // Check if essential data for logging is present
            if (CurrentUserId <= 0 || string.IsNullOrEmpty(CurrentUsername))
            {
                Debug.WriteLine($"Warning: Cannot log activity '{activityType}' due to missing UserID or Username.");
                return;
            }

            try
            {
                // Simple table creation check (idempotent) - Consider doing this once at startup
                string createTableQuery = @"
                    CREATE TABLE IF NOT EXISTS user_activity_log (
                        id INT AUTO_INCREMENT PRIMARY KEY,
                        user_id INT NULL,
                        username VARCHAR(100) NULL,
                        user_role VARCHAR(50) NULL,
                        activity_type VARCHAR(50) NOT NULL,
                        activity_time DATETIME NOT NULL,
                        session_token VARCHAR(100) NULL,
                        ip_address VARCHAR(50) NULL,
                        INDEX idx_user_activity_log (user_id),
                        INDEX idx_activity_time_log (activity_time)
                    );";
                DatabaseConnection.ExecuteNonQuery(createTableQuery);


                var logParams = new Dictionary<string, object>
                {
                    { "@UserId", CurrentUserId },
                    { "@Username", CurrentUsername },
                    { "@UserRole", CurrentUserRole ?? "N/A" }, // Handle potential null
                    { "@ActivityType", activityType },
                    { "@ActivityTime", DateTime.Now },
                    { "@SessionToken", SessionToken ?? "N/A" }, // Handle potential null
                    { "@IpAddress", GetUserIpAddress() } // Get IP address
                };

                string logQuery = @"
                    INSERT INTO user_activity_log
                        (user_id, username, user_role, activity_type, activity_time, session_token, ip_address)
                    VALUES
                        (@UserId, @Username, @UserRole, @ActivityType, @ActivityTime, @SessionToken, @IpAddress);";

                DatabaseConnection.ExecuteNonQuery(logQuery, logParams);
            }
            catch (Exception ex)
            {
                // Log database errors silently to avoid disrupting the user
                Debug.WriteLine($"Error logging activity '{activityType}' to database: {ex.Message}");
            }
        }

        /// <summary>
        /// Placeholder method to get user IP Address. Replace with actual implementation if needed.
        /// </summary>
        private static string GetUserIpAddress()
        {
            // Basic placeholder - real implementation depends on deployment (web vs desktop)
            return "127.0.0.1";
        }
    }
}