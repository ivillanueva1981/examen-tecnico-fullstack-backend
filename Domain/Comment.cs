using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain
{
    public  class Comment
    {
        public int Id { get; set; } // NO es Identity en tu script
        public int TicketId { get; set; }
        public string Text { get; set; } = string.Empty; // Mapeado a [Text] nvarchar(2000)
        public DateTime CreatedAt { get; set; }

        // Relación con el Creador (User)
        public int CreatedBy { get; set; }
        public User? Creator { get; set; }

        public Ticket? Ticket { get; set; }
    }
}
