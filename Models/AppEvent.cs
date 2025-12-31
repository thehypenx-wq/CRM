using System;

namespace OfficeSuite.Models
{
    public class AppEvent
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public List<Attachment> Attachments { get; set; } = new List<Attachment>();
        public DateTime CreatedDate { get; set; }
    }
}
