using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FavorisModels
{
    /// <summary>
    /// نموذج بيانات المفضلة
    /// </summary>
    public class Favoris
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public int TheseId { get; set; }
    }
}
