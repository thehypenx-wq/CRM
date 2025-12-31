using System;

namespace OfficeSuite.Models
{
    public class Todo
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Title { get; set; } = string.Empty;
        public bool IsCompleted { get; set; }
        public DateTime? DueDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public int? ProjectId { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        public int? AssignedToUserId { get; set; }
        public string AssignedToName { get; set; } = string.Empty;
        public string Remark { get; set; } = string.Empty;
        public int? StatusId { get; set; }
        public string StatusName { get; set; } = string.Empty;
        public string StatusColor { get; set; } = string.Empty;
        public int? CreatedBy { get; set; }
        public string CreatedByName { get; set; } = string.Empty;
        public List<Attachment> Attachments { get; set; } = new List<Attachment>();
        public List<TodoComment> Comments { get; set; } = new List<TodoComment>();
    }
}
