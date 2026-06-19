using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain;
    public class Ticket
    {
        public int Id { get; set; } // Es IDENTITY(1,1)
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }

        // Relación con el Creador (User)
        public int CreatedBy { get; set; }
        public User? Creator { get; set; }

        // Relación con sus comentarios
        public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    }