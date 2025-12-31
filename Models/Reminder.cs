using System;

namespace OfficeSuite.Models
{
    public class Reminder
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string ClientName { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime ReminderDate { get; set; }
        public bool IsSent { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
