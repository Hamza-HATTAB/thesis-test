using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ThesesModels // Ensure this namespace matches references
{
    /// <summary>
    /// Represents a Thesis or Dissertation entity.
    /// Maps to the `theses` table in the database.
    /// </summary>
    public class Theses
    {
        [Key] // Primary Key
        public int Id { get; set; }

        [Required]
        [MaxLength(255)]
        [Column("titre")] // Explicit mapping (optional if names match)
        public string Titre { get; set; }

        [Required]
        [Column("Resume")] // Maps to the database 'Resume' column
        public string Resume { get; set; } // Use this property, NOT Description

        [Required]
        [MaxLength(100)]
        [Column("speciality")]
        public string Speciality { get; set; }

        [Required]
        [MaxLength(50)] // Based on enum('Doctorat','Master')
        [Column("Type")]
        public string Type { get; set; } // Store as string, matching DB enum

        [Required]
        [Column("mots_cles")]
        public string Mots_cles { get; set; }

        [Required]
        [Column("annee")] // Database column is DATETIME
        public DateTime Annee { get; set; } // Use this property, NOT DateAdded

        [Required]
        [MaxLength(255)]
        [Column("fichier")] // Database column stores the path/URL
        public string Fichier { get; set; }

        [Required]
        [Column("user_id")] // Foreign key to users table
        public int UserId { get; set; }

        // --- Not Mapped Properties ---
        // These are populated manually, often via JOINs in queries.

        /// <summary>
        /// Author's name, typically retrieved via a JOIN with the 'users' table.
        /// Not stored directly in the 'theses' table according to the provided schema dump.
        /// </summary>
        [NotMapped]
        public string Auteur { get; set; }

        /// <summary>
        /// The ID of the corresponding entry in the 'favoris' table, if this thesis is favorited by the current user.
        /// Used primarily for delete operations from the favorites view.
        /// </summary>
        [NotMapped]
        public int FavorisId { get; set; } // Used in FavoritesView

        /// <summary>
        /// Convenience property mapping to the 'Annee' database field (which is DATETIME).
        /// Provides a clearer name for the publication date/year aspect.
        /// </summary>
        [NotMapped]
        public DateTime DatePublication
        {
            get { return Annee; }
            set { Annee = value; }
        }

        /// <summary>
        /// Convenience property mapping to the 'Fichier' database field.
        /// Provides a clearer name for the file path/URL.
        /// </summary>
        [NotMapped]
        public string FichierUrl
        {
            get { return Fichier; }
            set { Fichier = value; }
        }
    }
}