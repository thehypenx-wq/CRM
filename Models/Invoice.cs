using System;

namespace OfficeSuite.Models
{
    public class Invoice
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int ClientId { get; set; }
        public string ClientName { get; set; } = string.Empty;
        public string ClientEmail { get; set; } = string.Empty;
        public string ServiceType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateTime RenewalDate { get; set; }
        public bool IsPaid { get; set; }
        public bool IsDeleted { get; set; }
    }
}
