using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GroupeV
{
    /// <summary>
    /// Pre-sale entity
    /// </summary>
    [Table("prevente")]
    public class Prevente
    {
        [Key]
        [Column("id_prevente")]
        public int IdPrevente { get; set; }

        [Column("date_limite")]
        public DateTime? DateLimite { get; set; }

        [Column("nombre_min")]
        public int? NombreMin { get; set; }

        [Column("statut")]
        [MaxLength(255)]
        public string? Statut { get; set; }

        [Column("prix_prevente")]
        public decimal? PrixPrevente { get; set; }

        [Column("id_produit")]
        public int? IdProduit { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }

        // Navigation property
        [ForeignKey("IdProduit")]
        public virtual Produit? Produit { get; set; }

        // Calculated properties
        [NotMapped]
        public string PrixFormate => PrixPrevente.HasValue ? $"{PrixPrevente.Value:N2} DH" : "N/A";

        [NotMapped]
        public bool EstActif => DateLimite.HasValue && DateLimite.Value > DateTime.Now;

        [NotMapped]
        public string StatusFormate
        {
            get
            {
                if (string.IsNullOrEmpty(Statut))
                    return "En attente";
                return Statut;
            }
        }
    }
}
