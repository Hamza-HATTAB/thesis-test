using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows; // Only if using MessageBox directly (avoid if possible)
using MySql.Data.MySqlClient;
using ThesesModels; // For Theses model
using DataGridNamespace; // For DatabaseConnection
using System.Diagnostics; // For Debug

namespace DataGridNamespace.DataProviders
{
    /// <summary>
    /// Provides data access methods related to Theses. Handles CRUD operations.
    /// </summary>
    public class ThesisDataProvider
    {
        /// <summary>
        /// Retrieves a paginated and filtered list of theses, including author names.
        /// </summary>
        public List<Theses> GetTheses(int page, int itemsPerPage, string searchTerm, string thesisTypeFilter, out int totalItems)
        {
            List<Theses> thesesList = new List<Theses>();
            totalItems = 0;

            // Base queries joining theses with users to get author name
            string baseDataQuery = @"
                SELECT t.id, t.titre, u.nom AS AuteurName, t.speciality, t.Type, t.mots_cles, t.annee, t.Resume, t.fichier, t.user_id
                FROM theses t
                JOIN users u ON t.user_id = u.id"; // Join users table
            string baseCountQuery = @"
                SELECT COUNT(t.id)
                FROM theses t
                JOIN users u ON t.user_id = u.id";

            string whereClause = "";
            var parameters = new Dictionary<string, object>();

            // Apply Type Filter
            if (thesisTypeFilter != "All Types" && !string.IsNullOrEmpty(thesisTypeFilter))
            {
                whereClause += " WHERE t.Type = @Type";
                parameters.Add("@Type", thesisTypeFilter);
            }

            // Apply Search Filter (Title or Author Name)
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                string searchCondition = " (t.titre LIKE @SearchText OR u.nom LIKE @SearchText) ";
                whereClause += string.IsNullOrEmpty(whereClause) ? " WHERE" : " AND";
                whereClause += searchCondition;
                parameters.Add("@SearchText", $"%{searchTerm}%");
            }

            // --- Execute Count Query ---
            string finalCountQuery = baseCountQuery + whereClause;
            try
            {
                var countParams = new Dictionary<string, object>(parameters); // Copy params for count
                var countResult = DatabaseConnection.ExecuteScalar(finalCountQuery, countParams);
                totalItems = countResult != null ? Convert.ToInt32(countResult) : 0;
            }
            catch (Exception ex)
            {
                LogDataError("Error counting theses.", ex);
                return thesesList; // Return empty list on count error
            }

            // --- Execute Data Query ---
            string finalDataQuery = baseDataQuery + whereClause + " ORDER BY t.annee DESC, t.titre ASC LIMIT @Offset, @Limit";
            parameters.Add("@Offset", (page - 1) * itemsPerPage);
            parameters.Add("@Limit", itemsPerPage);

            try
            {
                using (var reader = DatabaseConnection.ExecuteReader(finalDataQuery, parameters))
                {
                    if (reader == null) { LogDataError("DB connection failed while fetching theses."); return thesesList; }
                    while (reader.Read())
                    {
                        thesesList.Add(new Theses
                        {
                            Id = reader.GetInt32("id"),
                            Titre = reader.GetString("titre"),
                            Auteur = reader.GetString("AuteurName"), // From JOIN
                            Speciality = reader.GetString("speciality"),
                            Type = reader.GetString("Type"),
                            Mots_cles = reader.GetString("mots_cles"),
                            Annee = reader.GetDateTime("annee"),         // FIXED: Use Annee
                            Resume = reader.GetString("Resume"),       // FIXED: Use Resume
                            Fichier = reader.GetString("fichier"),
                            UserId = reader.GetInt32("user_id")
                            // NotMapped properties DatePublication/FichierUrl are handled by the model
                        });
                    }
                }
            }
            catch (Exception ex) { LogDataLoadError(ex, "theses list"); } // Use specific handler

            return thesesList;
        }

        /// <summary>
        /// Adds a new thesis record to the database.
        /// </summary>
        public bool AddThesis(Theses thesis)
        {
            if (thesis == null) { Debug.WriteLine("AddThesis failed: thesis object was null."); return false; }

            try
            {
                // Query uses actual database column names
                string query = @"INSERT INTO theses
                                 (titre, auteur, speciality, Type, mots_cles, annee, Resume, fichier, user_id)
                                 VALUES
                                 (@Titre, @Auteur, @Specialty, @Type, @Keywords, @Annee, @Resume, @Fichier, @UserId)";

                var parameters = new Dictionary<string, object> {
                    { "@Titre", thesis.Titre },
                    { "@Auteur", thesis.Auteur }, // Assumes storing author name here too
                    { "@Specialty", thesis.Speciality },
                    { "@Type", thesis.Type },
                    { "@Keywords", thesis.Mots_cles },
                    { "@Annee", thesis.Annee },        // FIXED: Use Annee (which is DateTime)
                    { "@Resume", thesis.Resume },      // FIXED: Use Resume
                    { "@Fichier", thesis.Fichier },
                    { "@UserId", thesis.UserId }
                };

                int rowsAffected = DatabaseConnection.ExecuteNonQuery(query, parameters);
                if (rowsAffected > 0) { Debug.WriteLine($"Successfully added Thesis ID (potentially {thesis.Id}, depends on return): {thesis.Titre}"); }
                else { Debug.WriteLine($"Failed to add Thesis: {thesis.Titre}. No rows affected."); }
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                LogDataError("Error adding thesis.", ex);
                return false;
            }
        }

        /// <summary>
        /// Updates an existing thesis record in the database.
        /// </summary>
        public bool UpdateThesis(Theses thesis)
        {
            if (thesis == null || thesis.Id <= 0) { Debug.WriteLine("UpdateThesis failed: Invalid thesis object or ID."); return false; }

            try
            {
                string query = @"UPDATE theses SET
                                     titre = @Titre, auteur = @Auteur, speciality = @Specialty, Type = @Type,
                                     mots_cles = @Keywords, annee = @Annee, Resume = @Resume, fichier = @Fichier, user_id = @UserId
                                 WHERE id = @Id";

                var parameters = new Dictionary<string, object> {
                    { "@Titre", thesis.Titre },
                    { "@Auteur", thesis.Auteur },
                    { "@Specialty", thesis.Speciality },
                    { "@Type", thesis.Type },
                    { "@Keywords", thesis.Mots_cles },
                    { "@Annee", thesis.Annee },         // FIXED: Use Annee
                    { "@Resume", thesis.Resume },       // FIXED: Use Resume
                    { "@Fichier", thesis.Fichier },
                    { "@UserId", thesis.UserId },
                    { "@Id", thesis.Id }
                };

                int rowsAffected = DatabaseConnection.ExecuteNonQuery(query, parameters);
                if (rowsAffected > 0) { Debug.WriteLine($"Successfully updated Thesis ID: {thesis.Id}"); }
                else { Debug.WriteLine($"Failed to update Thesis ID: {thesis.Id}. ID not found or data unchanged."); }
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                LogDataError($"Error updating thesis ID: {thesis.Id}", ex);
                return false;
            }
        }

        /// <summary>
        /// Deletes a thesis record from the database by its ID.
        /// </summary>
        public bool DeleteThesis(int thesisId)
        {
            if (thesisId <= 0) { Debug.WriteLine("DeleteThesis failed: Invalid ID."); return false; }

            try
            {
                // Consider implications of ON DELETE CASCADE in favoris/contacts tables
                string query = "DELETE FROM theses WHERE id = @Id";
                var parameters = new Dictionary<string, object> { { "@Id", thesisId } };
                int rowsAffected = DatabaseConnection.ExecuteNonQuery(query, parameters);
                if (rowsAffected > 0) { Debug.WriteLine($"Successfully deleted Thesis ID: {thesisId}"); }
                else { Debug.WriteLine($"Failed to delete Thesis ID: {thesisId}. ID not found."); }
                return rowsAffected > 0;
            }
            catch (MySqlException dbEx) when (dbEx.Number == 1451) // Foreign key constraint violation
            {
                LogDataError($"Cannot delete thesis ID: {thesisId}. It is referenced by other records.", dbEx, isWarning: true);
                // Optionally re-throw or return specific error code if caller needs to know *why* it failed
                return false;
            }
            catch (Exception ex)
            {
                LogDataError($"Error deleting thesis ID: {thesisId}", ex);
                return false;
            }
        }

        /// <summary>
        /// Retrieves a paginated list of theses favorited by a specific user.
        /// </summary>
        public List<Theses> GetFavoriteTheses(int userId, int page, int itemsPerPage, string searchTerm, string thesisTypeFilter, out int totalItems)
        {
            List<Theses> favoritesList = new List<Theses>();
            totalItems = 0;
            if (userId <= 0) return favoritesList; // Invalid user

            string baseDataQuery = @"
                SELECT t.id, t.titre, u.nom AS AuteurName, t.speciality, t.Type, t.mots_cles, t.annee, t.Resume, t.fichier, t.user_id,
                       f.id AS FavoriteRecordId
                FROM favoris f JOIN theses t ON f.these_id = t.id JOIN users u ON t.user_id = u.id
                WHERE f.user_id = @UserId";
            string baseCountQuery = @"
                SELECT COUNT(f.id) FROM favoris f JOIN theses t ON f.these_id = t.id JOIN users u ON t.user_id = u.id
                WHERE f.user_id = @UserId";

            string whereClause = "";
            var parameters = new Dictionary<string, object> { { "@UserId", userId } };

            // Apply Filters
            if (thesisTypeFilter != "All Types") { whereClause += " AND t.Type = @Type"; parameters.Add("@Type", thesisTypeFilter); }
            if (!string.IsNullOrWhiteSpace(searchTerm)) { whereClause += " AND (t.titre LIKE @SearchText OR u.nom LIKE @SearchText)"; parameters.Add("@SearchText", $"%{searchTerm}%"); }

            // Get Count
            string finalCountQuery = baseCountQuery + whereClause;
            try
            {
                var countParams = new Dictionary<string, object>(parameters);
                totalItems = Convert.ToInt32(DatabaseConnection.ExecuteScalar(finalCountQuery, countParams));
            }
            catch (Exception ex) { LogDataError("Error counting favorites.", ex); return favoritesList; }

            // Get Data
            string finalDataQuery = baseDataQuery + whereClause + " ORDER BY f.id ASC LIMIT @Offset, @Limit";
            parameters.Add("@Offset", (page - 1) * itemsPerPage); parameters.Add("@Limit", itemsPerPage);
            try
            {
                using (var reader = DatabaseConnection.ExecuteReader(finalDataQuery, parameters))
                {
                    if (reader == null) { LogDataError("DB connection failed for favorites."); return favoritesList; }
                    while (reader.Read())
                    {
                        favoritesList.Add(new Theses
                        {
                            Id = reader.GetInt32("id"),
                            Titre = reader.GetString("titre"),
                            Auteur = reader.GetString("AuteurName"),
                            Speciality = reader.GetString("speciality"),
                            Type = reader.GetString("Type"),
                            Mots_cles = reader.GetString("mots_cles"),
                            Annee = reader.GetDateTime("annee"),
                            Resume = reader.GetString("Resume"),
                            Fichier = reader.GetString("fichier"),
                            UserId = reader.GetInt32("user_id"),
                            FavorisId = reader.GetInt32("FavoriteRecordId")
                        });
                    }
                }
            }
            catch (Exception ex) { LogDataLoadError(ex, "favorites list"); }
            return favoritesList;
        }

        private void LogDataError(string v)
        {
            throw new NotImplementedException();
        }

        /// <summary> Adds a thesis to a user's favorites list. </summary>
        public bool AddFavorite(int userId, int thesisId)
        {
            if (userId <= 0 || thesisId <= 0) return false;
            try
            {
                string checkQuery = "SELECT COUNT(*) FROM favoris WHERE user_id = @UserId AND these_id = @ThesisId";
                var checkParams = new Dictionary<string, object> { { "@UserId", userId }, { "@ThesisId", thesisId } };
                if (Convert.ToInt32(DatabaseConnection.ExecuteScalar(checkQuery, checkParams)) > 0)
                {
                    Debug.WriteLine($"Favorite already exists: Uid={userId}, Tid={thesisId}."); return true; // Indicate success as it exists
                }
                string query = "INSERT INTO favoris (user_id, these_id) VALUES (@UserId, @ThesisId)";
                var parameters = new Dictionary<string, object> { { "@UserId", userId }, { "@ThesisId", thesisId } };
                int rowsAffected = DatabaseConnection.ExecuteNonQuery(query, parameters);
                if (rowsAffected > 0) Debug.WriteLine($"Added favorite: Uid={userId}, Tid={thesisId}.");
                return rowsAffected > 0;
            }
            catch (Exception ex) { LogDataError("Error adding favorite.", ex); return false; }
        }

        /// <summary> Removes a favorite record based on its own ID and the user ID. </summary>
        public bool RemoveFavorite(int favoriteRecordId, int userId)
        {
            if (favoriteRecordId <= 0 || userId <= 0) return false;
            try
            {
                string query = "DELETE FROM favoris WHERE id = @FavoriteId AND user_id = @UserId";
                var parameters = new Dictionary<string, object> { { "@FavoriteId", favoriteRecordId }, { "@UserId", userId } };
                int rowsAffected = DatabaseConnection.ExecuteNonQuery(query, parameters);
                if (rowsAffected > 0) Debug.WriteLine($"Removed favorite record ID: {favoriteRecordId} for User ID: {userId}.");
                else Debug.WriteLine($"Failed to remove favorite record ID: {favoriteRecordId}. Not found or permission denied.");
                return rowsAffected > 0;
            }
            catch (Exception ex) { LogDataError("Error removing favorite.", ex); return false; }
        }

        // --- Logging Helpers ---
        private void LogDataError(string message, Exception ex, bool isWarning = false)
        {
            string level = isWarning ? "WARNING" : "ERROR";
            Debug.WriteLine($"[{level}] ThesisDataProvider: {message}" + (ex != null ? $"\n{ex}" : ""));
            // Avoid showing MessageBox directly from DataProvider
        }
        private void LogDataLoadError(Exception ex, string context) => LogDataError($"Error loading {context}.", ex);

    } // End Class
} // End Namespace