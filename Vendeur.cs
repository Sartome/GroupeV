using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GroupeV
{
    /// <summary>
    /// Seller entity - inherits from utilisateur
    /// </summary>
    [Table("vendeur")]
    public class Vendeur
    {
        [Key]
        [Column("id_user")]
        public int IdUser { get; set; }

        [Column("nom_entreprise")]
        [MaxLength(100)]
        public string? NomEntreprise { get; set; }

        [Column("siret")]
        [MaxLength(14)]
        public string? Siret { get; set; }

        [Column("adresse_entreprise")]
        [MaxLength(100)]
        public string? AdresseEntreprise { get; set; }

        [Column("email_pro")]
        [MaxLength(100)]
        public string? EmailPro { get; set; }

        [Column("is_certified")]
        public bool IsCertified { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }

        // Navigation property to user base info
        [ForeignKey("IdUser")]
        public virtual Utilisateur? Utilisateur { get; set; }

        // Calculated properties
        [NotMapped]
        public string NomComplet => Utilisateur != null ? $"{Utilisateur.Prenom} {Utilisateur.Nom}" : "Unknown";

        [NotMapped]
        public string StatusCertification => IsCertified ? "? Certifié" : "? Non certifié";
    }
}
