using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GroupeV
{
    /// <summary>
    /// Base user entity representing utilisateur table
    /// </summary>
    [Table("utilisateur")]
    public class Utilisateur
    {
        [Key]
        [Column("id_user")]
        public int IdUser { get; set; }

        [Column("nom")]
        [MaxLength(255)]
        public string? Nom { get; set; }

        [Column("prenom")]
        [MaxLength(255)]
        public string? Prenom { get; set; }

        [Column("adresse")]
        [MaxLength(255)]
        public string? Adresse { get; set; }

        [Column("phone")]
        [MaxLength(20)]
        public string? Phone { get; set; }

        [Column("avatar")]
        [MaxLength(255)]
        public string? Avatar { get; set; }

        [Column("email")]
        [MaxLength(255)]
        public string? Email { get; set; }

        [Column("motdepasse")]
        [MaxLength(255)]
        public string? MotDePasse { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }

        [NotMapped]
        public string NomComplet => $"{Prenom} {Nom}";
    }
}
