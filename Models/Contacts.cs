using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
// Removed unused using directives for UserModels and ThesesModels as direct FK properties are often handled by ORM or explicit joins
// If using an ORM like EF Core, you would keep them and configure the relationships. For direct ADO.NET/Dapper style, they aren't needed here.

namespace ContactsModels // Keep original namespace
{
    public class Contacts
    {
        [Key] // Primary Key
        public int Id { get; set; }

        [Required] // Foreign key to the user who sent the message
        [Column("user_id")] // Explicitly map to the database column name
        public int UserId { get; set; }
        // Removed the virtual User Expediteur property - requires ORM setup

        [Required] // Foreign key to the thesis being discussed
        [Column("these_id")] // Explicitly map to the database column name
        public int TheseId { get; set; }
        // Removed the virtual Theses These property - requires ORM setup

        // Removed DestinataireId and related properties as they don't exist in the DB schema provided
        // [Required]
        // public int DestinataireId { get; set; }
        // [ForeignKey("DestinataireId")]
        // public virtual User Destinataire { get; set; }

        [Required] // Message content
        [Column("message")] // Explicitly map to the database column name
        public string Message { get; set; }

        [Required] // Date sent (automatically set by DB default)
        [Column("date_envoi")] // Explicitly map to the database column name
        public DateTime DateEnvoi { get; set; } // Default value is handled by DB
    }
}