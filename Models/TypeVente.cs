namespace GroupeV.Models;

/// <summary>
/// Type de vente pour un produit.
/// </summary>
public enum TypeVente
{
    /// <summary>
    /// Vente standard (prix fixe)
    /// </summary>
    Standard = 0,
    
    /// <summary>
    /// Vente en groupe avec réduction selon quantité
    /// </summary>
    VenteGroupe = 1,
    
    /// <summary>
    /// Vente aux enchères (bid)
    /// </summary>
    Enchere = 2
}

/// <summary>
/// Extensions pour TypeVente
/// </summary>
public static class TypeVenteExtensions
{
    public static string ToDisplayString(this TypeVente type) => type switch
    {
        TypeVente.Standard => "🛒 Vente Standard",
        TypeVente.VenteGroupe => "👥 Vente Groupe",
        TypeVente.Enchere => "🔨 Enchère",
        _ => "Standard"
    };

    public static string ToBadgeText(this TypeVente type) => type switch
    {
        TypeVente.Standard    => "Achat direct",
        TypeVente.VenteGroupe => "Vente groupée",
        TypeVente.Enchere     => "Enchère",
        _                     => "Standard"
    };
}
