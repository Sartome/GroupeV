using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GroupeV.Models
{
    /// <summary>
    /// A message inside a support ticket conversation.
    /// </summary>
    [Table("ticket_message")]
    public class TicketMessage
    {
        [Key]
        [Column("id_message")]
        public int IdMessage { get; set; }

        [Column("id_ticket")]
        public int IdTicket { get; set; }

        [Column("id_vendeur")]
        public int IdVendeur { get; set; }

        [Column("message")]
        public string Message { get; set; } = string.Empty;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        public virtual Vendeur? Vendeur { get; set; }

        [NotMapped]
        public string HeureFormate => CreatedAt.ToString("dd/MM HH:mm");

        [NotMapped]
        public bool IsFromCurrentUser =>
            IdVendeur == AuthenticationService.CurrentSeller?.IdUser;

        [NotMapped]
        public string ExpéditeurNom => Vendeur?.NomComplet ?? $"Vendeur #{IdVendeur}";
    }
}
