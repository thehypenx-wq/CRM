using System;

namespace OfficeSuite.Models
{
    public class TodoComment
    {
        public int Id { get; set; }
        public int TodoId { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Comment { get; set; } = string.Empty;
        public string? AttachmentPath { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
