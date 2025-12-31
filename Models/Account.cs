using System;

namespace OfficeSuite.Models
{
    public class Account
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Currency { get; set; } = "USD";
        public decimal Balance { get; set; }
        public int UserId { get; set; }
        public DateTime CreatedAt { get; set; }
        
        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public string SharedWithNames { get; set; } = string.Empty;
    }
}
