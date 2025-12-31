using System;

namespace OfficeSuite.Models
{
    public class Transaction
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Description { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Type { get; set; } = string.Empty; // Income, Expense
        public DateTime TransactionDate { get; set; }
        public DateTime CreatedDate { get; set; }
        public int? AccountId { get; set; }
        public string AccountName { get; set; } = string.Empty;
        public string CreatedByName { get; set; } = string.Empty;
    }

    public class TransactionSummary 
    {
        public decimal TotalIncome { get; set; }
        public decimal TotalExpense { get; set; }
        public decimal Balance { get; set; }
        public List<Transaction> Transactions { get; set; } = new List<Transaction>();
    }
}
