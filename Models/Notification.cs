using System;

namespace OfficeSuite.Models
{
    public class Notification
    {
        public int Id { get; set; }
        public int? UserId { get; set; } // Null for public/broadcast
        public string Message { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // 'Chat', 'Todo', 'Planner'
        public int? RelatedEntityId { get; set; }
        public string RelatedEntityName { get; set; } = string.Empty;
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
        public int? CreatedBy { get; set; }
        public string CreatedByName { get; set; } = string.Empty; // For display
    }
}
