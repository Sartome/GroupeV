using System.Windows;
using System.Windows.Controls;

namespace GroupeV.Converters;

/// <summary>
/// Converter pour obtenir la couleur de badge selon le type de vente.
/// </summary>
public static class TypeVenteHelper
{
    /// <summary>
    /// Retourne la couleur du badge selon le type de vente.
    /// </summary>
    public static string GetBadgeColor(int typeVente)
    {
        return typeVente switch
        {
            0 => "#10b981", // NeuSuccessBrush - Standard (vert)
            1 => "#f59e0b", // NeuWarningBrush - Groupe (orange)
            2 => "#ef4444", // NeuDangerBrush - Enchère (rouge)
            _ => "#6366f1"  // NeuAccentBrush - Par défaut (bleu)
        };
    }

    /// <summary>
    /// Retourne l'icône selon le type de vente.
    /// </summary>
    public static string GetIcon(int typeVente)
    {
        return typeVente switch
        {
            0 => "🛒",
            1 => "👥",
            2 => "🔨",
            _ => "📦"
        };
    }

    /// <summary>
    /// Retourne une description du type de vente.
    /// </summary>
    public static string GetDescription(int typeVente)
    {
        return typeVente switch
        {
            0 => "Vente à prix fixe standard",
            1 => "Vente en groupe avec réduction selon quantité",
            2 => "Vente aux enchères avec offres en temps réel",
            _ => "Type de vente inconnu"
        };
    }

    /// <summary>
    /// Valide si le type de vente est supporté.
    /// </summary>
    public static bool IsValidTypeVente(int typeVente)
    {
        return typeVente is >= 0 and <= 2;
    }
}
