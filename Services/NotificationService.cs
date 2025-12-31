using OfficeSuite.Data;
using OfficeSuite.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace OfficeSuite.Services
{
    public class NotificationService
    {
        private readonly SqlHelper _db;

        public NotificationService(SqlHelper db)
        {
            _db = db;
        }

        public void AddNotification(int? userId, string message, string type, int? relatedEntityId, string relatedEntityName, int? createdBy)
        {
            string query = @"
                INSERT INTO Notifications (UserId, Message, Type, RelatedEntityId, RelatedEntityName, CreatedBy)
                VALUES (@UserId, @Message, @Type, @RelatedEntityId, @RelatedEntityName, @CreatedBy)";

            _db.ExecuteNonQuery(query, new SqlParameter[] {
                new SqlParameter("@UserId", userId ?? (object)DBNull.Value),
                new SqlParameter("@Message", message),
                new SqlParameter("@Type", type),
                new SqlParameter("@RelatedEntityId", relatedEntityId ?? (object)DBNull.Value),
                new SqlParameter("@RelatedEntityName", relatedEntityName ?? (object)DBNull.Value),
                new SqlParameter("@CreatedBy", createdBy ?? (object)DBNull.Value)
            });
        }

        public void MarkAllAsRead(int userId)
        {
            // Fix: Include NULL UserId rows so global notifications get marked as read too
            string query = "UPDATE Notifications SET IsRead = 1 WHERE (UserId = @UserId OR UserId IS NULL) AND IsRead = 0";
            _db.ExecuteNonQuery(query, new SqlParameter[] { new SqlParameter("@UserId", userId) });
        }

        public List<Notification> GetUnreadNotifications(int userId)
        {
            string query = @"
                SELECT n.*, u.Username as CreatedByName 
                FROM Notifications n
                LEFT JOIN Users u ON n.CreatedBy = u.Id
                WHERE (n.UserId = @UserId OR n.UserId IS NULL) AND n.IsRead = 0
                ORDER BY n.CreatedAt DESC";

            var dt = _db.ExecuteQuery(query, new SqlParameter[] { new SqlParameter("@UserId", userId) });
            var list = new List<Notification>();
            foreach (DataRow row in dt.Rows)
            {
                list.Add(MapToNotification(row));
            }
            return list;
        }

        public List<Notification> GetRecentActivity()
        {
            // Public feed of actions (UserId is NULL usually for public, or we show all non-private ones?)
            // For now, let's show all notifications that are 'System' or public, or just latest 10 for the user?
            // "Activity Feed" usually implies what OTHERS did.
            string query = @"
                SELECT TOP 20 n.*, u.Username as CreatedByName 
                FROM Notifications n
                LEFT JOIN Users u ON n.CreatedBy = u.Id
                ORDER BY n.CreatedAt DESC"; // Show all for admin/dashboard demo

            var dt = _db.ExecuteQuery(query);
            var list = new List<Notification>();
            foreach (DataRow row in dt.Rows)
            {
                list.Add(MapToNotification(row));
            }
            return list;
        }

        private Notification MapToNotification(DataRow row)
        {
            return new Notification
            {
                Id = (int)row["Id"],
                UserId = row["UserId"] != DBNull.Value ? (int)row["UserId"] : (int?)null,
                Message = row["Message"].ToString(),
                Type = row["Type"].ToString(),
                RelatedEntityId = row["RelatedEntityId"] != DBNull.Value ? (int)row["RelatedEntityId"] : (int?)null,
                RelatedEntityName = row["RelatedEntityName"].ToString(),
                IsRead = (bool)row["IsRead"],
                CreatedAt = (DateTime)row["CreatedAt"],
                CreatedBy = row["CreatedBy"] != DBNull.Value ? (int)row["CreatedBy"] : (int?)null,
                CreatedByName = row["CreatedByName"].ToString()
            };
        }
    }
}
