using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GroupeV.Models;

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

        [Column("quantity")]
        public int Quantity { get; set; } = 1;

        [Column("prix_ht")]
        public decimal? PrixHt { get; set; }

        [Column("taux_tva")]
        public decimal TauxTva { get; set; } = 20.00m;

        [Column("sale_type")]
        public string SaleType { get; set; } = "buy";

        /// <summary>
        /// Integer representation used by the UI (0=buy, 1=group, 2=auction).
        /// </summary>
        [NotMapped]
        public int TypeVente
        {
            get => SaleType switch { "buy" => 0, "group" => 1, "auction" => 2, _ => 0 };
            set => SaleType = value switch { 0 => "buy", 1 => "group", 2 => "auction", _ => "buy" };
        }

        /// <summary>
        /// Minimum number of buyers required for a group sale.
        /// </summary>
        [Column("group_required_buyers")]
        public int? GroupRequiredBuyers { get; set; }

        /// <summary>
        /// Expiry datetime — auction end time or group sale deadline.
        /// </summary>
        [Column("group_expires_at")]
        public DateTime? GroupExpiresAt { get; set; }

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
        public string PrixHtFormate => PrixHt.HasValue ? $"{PrixHt.Value:N2} € HT" : "N/A";

        [NotMapped]
        public string StockBadge => Quantity > 5 ? "En stock" : Quantity > 0 ? "Stock faible" : "Rupture";

        [NotMapped]
        public bool IsInStock => Quantity > 0;

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
