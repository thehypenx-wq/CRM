using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OfficeSuite.Data;
using OfficeSuite.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace OfficeSuite.Controllers
{
    [Authorize]
    public class SearchController : Controller
    {
        private readonly SqlHelper _db;

        public SearchController(SqlHelper db)
        {
            _db = db;
        }

        public IActionResult Index(string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return RedirectToAction("Index", "Home");

            ViewBag.Query = query;
            var results = new SearchResultViewModel();
            var searchParam = $"%{query}%";

            // 1. Search Todos
            var todoDt = _db.ExecuteQuery("SELECT * FROM Todos WHERE (Title LIKE @q OR Remark LIKE @q) AND IsDeleted = 0", 
                new SqlParameter[] { new SqlParameter("@q", searchParam) });
            foreach (DataRow row in todoDt.Rows)
            {
                results.Todos.Add(new Todo { 
                    Id = row["Id"] != DBNull.Value ? (int)row["Id"] : 0, 
                    Title = row["Title"]?.ToString() ?? "" 
                });
            }

            // 2. Search Clients
            var clientDt = _db.ExecuteQuery("SELECT * FROM Clients WHERE (Name LIKE @q OR CompanyName LIKE @q OR Email LIKE @q) AND IsDeleted = 0", 
                new SqlParameter[] { new SqlParameter("@q", searchParam) });
            foreach (DataRow row in clientDt.Rows)
            {
                results.Clients.Add(new Client { 
                    Id = row["Id"] != DBNull.Value ? (int)row["Id"] : 0, 
                    Name = row["Name"]?.ToString() ?? "", 
                    CompanyName = row["CompanyName"]?.ToString() ?? "" 
                });
            }

            // 3. Search Invoices
            var invDt = _db.ExecuteQuery("SELECT i.*, c.Name as ClientName FROM Invoices i JOIN Clients c ON i.ClientId = c.Id WHERE (Description LIKE @q OR ServiceType LIKE @q) AND i.IsDeleted = 0", 
                new SqlParameter[] { new SqlParameter("@q", searchParam) });
            foreach (DataRow row in invDt.Rows)
            {
                results.Invoices.Add(new Invoice { 
                    Id = row["Id"] != DBNull.Value ? (int)row["Id"] : 0, 
                    Description = row["Description"]?.ToString() ?? "", 
                    ClientName = row["ClientName"]?.ToString() ?? "" 
                });
            }

            // 4. Search Projects
            var projDt = _db.ExecuteQuery("SELECT * FROM Projects WHERE Name LIKE @q OR Description LIKE @q", 
                new SqlParameter[] { new SqlParameter("@q", searchParam) });
            foreach (DataRow row in projDt.Rows)
            {
                results.Projects.Add(new Project { 
                    Id = row["Id"] != DBNull.Value ? (int)row["Id"] : 0, 
                    Name = row["Name"]?.ToString() ?? "" 
                });
            }

            // 5. Search Tickets
            var ticketDt = _db.ExecuteQuery("SELECT * FROM Tickets WHERE (Subject LIKE @q OR Description LIKE @q) AND IsDeleted = 0", 
                 new SqlParameter[] { new SqlParameter("@q", searchParam) });
            foreach (DataRow row in ticketDt.Rows)
            {
                results.Tickets.Add(new Ticket {
                    Id = row["Id"] != DBNull.Value ? (int)row["Id"] : 0,
                    Subject = row["Subject"]?.ToString() ?? "",
                    Status = row["Status"]?.ToString() ?? ""
                });
            }

            // 6. Search Transactions
             var transDt = _db.ExecuteQuery("SELECT * FROM Transactions WHERE Description LIKE @q", 
                 new SqlParameter[] { new SqlParameter("@q", searchParam) });
            foreach (DataRow row in transDt.Rows)
            {
                results.Transactions.Add(new Transaction {
                    Id = row["Id"] != DBNull.Value ? (int)row["Id"] : 0,
                    Description = row["Description"]?.ToString() ?? "",
                    Amount = row["Amount"] != DBNull.Value ? (decimal)row["Amount"] : 0,
                    Type = row["Type"]?.ToString() ?? ""
                });
            }

            return View(results);
        }
    }

    public class SearchResultViewModel
    {
        public List<Todo> Todos { get; set; } = new List<Todo>();
        public List<Client> Clients { get; set; } = new List<Client>();
        public List<Invoice> Invoices { get; set; } = new List<Invoice>();
        public List<Project> Projects { get; set; } = new List<Project>();
        public List<Ticket> Tickets { get; set; } = new List<Ticket>();
        public List<Transaction> Transactions { get; set; } = new List<Transaction>();
    }
}
