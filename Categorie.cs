using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GroupeV
{
    /// <summary>
    /// Product category entity
    /// </summary>
    [Table("categorie")]
    public class Categorie
    {
        [Key]
        [Column("id_categorie")]
        public int IdCategorie { get; set; }

        [Column("id_gestionnaire")]
        public int? IdGestionnaire { get; set; }

        [Column("lib")]
        [MaxLength(100)]
        public string? Libelle { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }
    }
}
