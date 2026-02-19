using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GroupeV.Models
{
    /// <summary>
    /// Support ticket created by a seller.
    /// </summary>
    [Table("ticket")]
    public class Ticket
    {
        [Key]
        [Column("id_ticket")]
        public int IdTicket { get; set; }

        [Column("titre")]
        [MaxLength(255)]
        public string Titre { get; set; } = string.Empty;

        [Column("id_vendeur")]
        public int IdVendeur { get; set; }

        [Column("statut")]
        [MaxLength(20)]
        public string Statut { get; set; } = "ouvert";

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }

        public virtual Vendeur? Vendeur { get; set; }
        public virtual ICollection<TicketMessage> Messages { get; set; } = new List<TicketMessage>();

        [NotMapped]
        public string StatutEmoji => Statut switch
        {
            "ouvert" => "🟢 Ouvert",
            "fermé" => "🔴 Fermé",
            "en_attente" => "🟡 En attente",
            _ => Statut
        };

        [NotMapped]
        public string DateFormate => CreatedAt.ToString("dd/MM/yyyy HH:mm");
    }
}
