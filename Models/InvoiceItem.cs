using System.ComponentModel.DataAnnotations.Schema;

namespace GroupeV;

/// <summary>
/// DTO pour l'affichage des factures par article.
/// </summary>
public sealed class InvoiceItem
{
    public int IdPrevente { get; init; }
    public string ProduitDescription { get; init; } = string.Empty;
    public decimal? PrixPrevente { get; init; }
    public DateTime? DateLimite { get; init; }
    public string Statut { get; init; } = string.Empty;
    public int? NombreMin { get; init; }
    public DateTime CreatedAt { get; init; }

    [NotMapped]
    public string PrixFormate => PrixPrevente.HasValue ? $"{PrixPrevente.Value:N2} €" : "N/A";

    [NotMapped]
    public string DateLimiteFormate => DateLimite?.ToString("dd/MM/yyyy") ?? "N/A";

    [NotMapped]
    public string StatutBadge => Statut switch
    {
        "En attente" => "⏳ En attente",
        "Validé" or "Validée" => "✓ Validée",
        "Annulé" or "Annulée" => "✕ Annulée",
        _ => Statut
    };
}
