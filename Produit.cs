using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GroupeV
{
    /// <summary>
    /// Product entity for sellers
    /// </summary>
    [Table("produit")]
    public class Produit
    {
        [Key]
        [Column("id_produit")]
        public int IdProduit { get; set; }

        [Column("description")]
        [MaxLength(255)]
        public string? Description { get; set; }

        [Column("prix")]
        public decimal? Prix { get; set; }

        [Column("image")]
        [MaxLength(255)]
        public string? Image { get; set; }

        [Column("image_alt")]
        [MaxLength(255)]
        public string? ImageAlt { get; set; }

        [Column("image_size")]
        public int? ImageSize { get; set; }

        [Column("image_width")]
        public int? ImageWidth { get; set; }

        [Column("image_height")]
        public int? ImageHeight { get; set; }

        [Column("id_vendeur")]
        public int? IdVendeur { get; set; }

        [Column("id_categorie")]
        public int? IdCategorie { get; set; }

        [Column("type_vente")]
        public int TypeVente { get; set; } = 0; // 0=Standard, 1=VenteGroupe, 2=Enchere

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }

        // Navigation properties
        [ForeignKey("IdVendeur")]
        public virtual Vendeur? Vendeur { get; set; }

        [ForeignKey("IdCategorie")]
        public virtual Categorie? Categorie { get; set; }

        // Calculated properties
        [NotMapped]
        public string PrixFormate => Prix.HasValue ? $"{Prix.Value:N2} €" : "N/A";

        [NotMapped]
        public string CategorieNom => Categorie?.Libelle ?? "Sans catégorie";

        [NotMapped]
        public string VendeurNom => Vendeur?.NomComplet ?? "Unknown";

        /// <summary>
        /// Retourne le type de vente typé.
        /// </summary>
        [NotMapped]
        public TypeVente TypeVenteEnum => (TypeVente)TypeVente;

        /// <summary>
        /// Retourne le type de vente formaté pour affichage.
        /// </summary>
        [NotMapped]
        public string TypeVenteDisplay => TypeVenteEnum.ToDisplayString();

        /// <summary>
        /// Retourne le texte du badge pour le type de vente.
        /// </summary>
        [NotMapped]
        public string TypeVenteBadge => TypeVenteEnum.ToBadgeText();

        /// <summary>
        /// Retourne le chemin d'image local s'il existe, sinon null.
        /// </summary>
        [NotMapped]
        public string? ImageAbsolutePath
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Image)) return null;
                if (System.IO.File.Exists(Image)) return Image;
                var localPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images", Image);
                return System.IO.File.Exists(localPath) ? localPath : null;
            }
        }

        [NotMapped]
        public bool HasImage => ImageAbsolutePath != null;
    }
}
