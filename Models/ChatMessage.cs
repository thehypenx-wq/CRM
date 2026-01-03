using System;

namespace OfficeSuite.Models
{
    public class ChatMessage
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string GroupName { get; set; } = string.Empty;
        public bool IsStarred { get; set; }
    }
}
