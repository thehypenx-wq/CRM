using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OfficeSuite.Data;
using OfficeSuite.Models;
using System.Security.Claims;
using Microsoft.Data.SqlClient;
using System.Data;

namespace OfficeSuite.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly SqlHelper _db;
    private readonly Services.NotificationService _notificationService;
    private readonly Services.PermissionService _permissionService;

    public HomeController(ILogger<HomeController> logger, SqlHelper db, Services.NotificationService notificationService, Services.PermissionService permissionService)
    {
        _logger = logger;
        _db = db;
        _notificationService = notificationService;
        _permissionService = permissionService;
    }

    private int? GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier);
        return claim != null ? int.Parse(claim.Value) : null;
    }

    public IActionResult Index()
    {
        if (!_permissionService.HasPermission("Dashboard"))
        {
            return RedirectToAction("Index", "Todo");
        }

        if (User.Identity?.IsAuthenticated == true)
        {
            var userId = GetUserId();
                            
            // Todos Stats - Global
            string todoQuery = "SELECT SUM(CASE WHEN IsCompleted = 1 THEN 1 ELSE 0 END) as Completed, SUM(CASE WHEN IsCompleted = 0 THEN 1 ELSE 0 END) as Pending FROM Todos WHERE IsDeleted = 0";
            var todoDt = _db.ExecuteQuery(todoQuery, new SqlParameter[] { });
            ViewBag.TodoCompleted = todoDt.Rows.Count > 0 && todoDt.Rows[0]["Completed"] != DBNull.Value ? (int)todoDt.Rows[0]["Completed"] : 0;
            ViewBag.TodoPending = todoDt.Rows.Count > 0 && todoDt.Rows[0]["Pending"] != DBNull.Value ? (int)todoDt.Rows[0]["Pending"] : 0;

            // Finance Stats
            string finQuery = "SELECT SUM(CASE WHEN Type='Income' THEN Amount ELSE 0 END) as Income, SUM(CASE WHEN Type='Expense' THEN Amount ELSE 0 END) as Expense FROM Transactions WHERE UserId = @UserId";
            var finDt = _db.ExecuteQuery(finQuery, new SqlParameter[] { new SqlParameter("@UserId", userId) });
            ViewBag.TotalIncome = finDt.Rows.Count > 0 && finDt.Rows[0]["Income"] != DBNull.Value ? (decimal)finDt.Rows[0]["Income"] : 0;
            ViewBag.TotalExpense = finDt.Rows.Count > 0 && finDt.Rows[0]["Expense"] != DBNull.Value ? (decimal)finDt.Rows[0]["Expense"] : 0;

            // Invoices Stats
            string invQuery = "SELECT SUM(CASE WHEN IsPaid = 1 THEN 1 ELSE 0 END) as Paid, SUM(CASE WHEN IsPaid = 0 THEN 1 ELSE 0 END) as Unpaid FROM Invoices WHERE UserId = @UserId";
            var invDt = _db.ExecuteQuery(invQuery, new SqlParameter[] { new SqlParameter("@UserId", userId) });
            ViewBag.InvPaid = invDt.Rows.Count > 0 && invDt.Rows[0]["Paid"] != DBNull.Value ? (int)invDt.Rows[0]["Paid"] : 0;
            ViewBag.InvUnpaid = invDt.Rows.Count > 0 && invDt.Rows[0]["Unpaid"] != DBNull.Value ? (int)invDt.Rows[0]["Unpaid"] : 0;

            // Monthly Trend (Last 6 months)
            string trendQuery = @"
                SELECT 
                    FORMAT(TransactionDate, 'MMM') as Month,
                    SUM(CASE WHEN Type='Income' THEN Amount ELSE 0 END) as Income,
                    SUM(CASE WHEN Type='Expense' THEN Amount ELSE 0 END) as Expense
                FROM Transactions 
                WHERE UserId = @UserId AND TransactionDate >= DATEADD(month, -5, GETDATE())
                GROUP BY FORMAT(TransactionDate, 'MMM'), YEAR(TransactionDate), MONTH(TransactionDate)
                ORDER BY YEAR(TransactionDate), MONTH(TransactionDate)";
            var trendDt = _db.ExecuteQuery(trendQuery, new SqlParameter[] { new SqlParameter("@UserId", userId) });
            
            var months = new List<string>();
            var incomes = new List<decimal>();
            var expenses = new List<decimal>();
            foreach (DataRow row in trendDt.Rows)
            {
                months.Add(row["Month"]?.ToString() ?? "");
                incomes.Add((decimal)row["Income"]);
                expenses.Add((decimal)row["Expense"]);
            }
            ViewBag.TrendMonths = months;
            ViewBag.TrendIncomes = incomes;
            ViewBag.TrendExpenses = expenses;

            // Activity Feed
            ViewBag.Activities = _notificationService.GetRecentActivity();

            // Recent Tasks (Top 5 Due or Pending)
            string recentTodoQuery = @"
                SELECT TOP 5 Title, DueDate, IsCompleted 
                FROM Todos 
                WHERE IsDeleted = 0 AND (UserId = @UserId OR AssignedToUserId = @UserId) AND IsCompleted = 0
                ORDER BY DueDate ASC";
            var todoRes = _db.ExecuteQuery(recentTodoQuery, new SqlParameter[] { new SqlParameter("@UserId", userId) });
            var recentTodos = new List<dynamic>();
            foreach(DataRow r in todoRes.Rows)
            {
                recentTodos.Add(new { Title = r["Title"].ToString(), Date = r["DueDate"] != DBNull.Value ? ((DateTime)r["DueDate"]).ToString("MM/dd") : "No Date" });
            }
            ViewBag.RecentTodos = recentTodos;

            // Recent Chat (Top 5 Last Messages)
            string recentChatQuery = "SELECT TOP 5 UserName, Message, Timestamp, GroupName FROM ChatMessages ORDER BY Timestamp DESC";
            var chatRes = _db.ExecuteQuery(recentChatQuery, null);
            var recentChats = new List<dynamic>();
            foreach(DataRow r in chatRes.Rows)
            {
                recentChats.Add(new { User = r["UserName"].ToString(), Msg = r["Message"].ToString(), Time = ((DateTime)r["Timestamp"]).ToString("HH:mm"), Group = r["GroupName"].ToString() });
            }
            ViewBag.RecentChats = recentChats;

            // Recent Tickets (Top 5 Open)
            string ticketQuery = @"
                SELECT TOP 5 Subject, Status, Priority, CreatedAt 
                FROM Tickets 
                WHERE IsDeleted = 0 AND Status != 'Closed'
                ORDER BY CreatedAt DESC"; 
                // Global tickets visible on dashboard? Or filtered by User? 
                // Requests usually imply seeing own work or team work. Let's filter by User or just show all for now if small team.
                // Re-reading task: "Recent Tickets Widget". I'll filter by User to be consistent with isolation, but maybe Admins see all.
                // Let's filter by User for now.
            ticketQuery = @"
                SELECT TOP 5 Subject, Status, Priority, CreatedAt 
                FROM Tickets 
                WHERE IsDeleted = 0 AND Status != 'Closed' AND UserId = @UserId
                ORDER BY CreatedAt DESC";

            var ticketRes = _db.ExecuteQuery(ticketQuery, new SqlParameter[] { new SqlParameter("@UserId", userId) });
            var recentTickets = new List<dynamic>();
            foreach(DataRow r in ticketRes.Rows)
            {
                recentTickets.Add(new { 
                    Subject = r["Subject"].ToString(), 
                    Status = r["Status"].ToString(),
                    Priority = r["Priority"].ToString(),
                    Date = ((DateTime)r["CreatedAt"]).ToString("MM/dd") 
                });
            }
            ViewBag.RecentTickets = recentTickets;
        }
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
