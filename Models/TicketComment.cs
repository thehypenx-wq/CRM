using System;

namespace OfficeSuite.Models
{
    public class TicketComment
    {
        public int Id { get; set; }
        public int TicketId { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty; // For display
        public string UserProfileImage { get; set; } // For display
        public string Comment { get; set; } = string.Empty;
        public string AttachmentPath { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
