using System.Data;
using Microsoft.Data.SqlClient;
using OfficeSuite.Data;
using System.Security.Claims;

namespace OfficeSuite.Services
{
    public class PermissionService
    {
        private readonly SqlHelper _db;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public PermissionService(SqlHelper db, IHttpContextAccessor httpContextAccessor)
        {
            _db = db;
            _httpContextAccessor = httpContextAccessor;
        }

        public bool HasPermission(string moduleName)
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user == null || !user.Identity.IsAuthenticated) return false;

            if (user.IsInRole("Admin")) return true;

            var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null) return false;

            int userId = int.Parse(userIdClaim.Value);

            string query = @"
                SELECT COUNT(1) 
                FROM UserPermissions up 
                JOIN AppModules am ON up.ModuleId = am.Id 
                WHERE up.UserId = @UserId AND am.ModuleName = @ModuleName";

            var result = _db.ExecuteScalar(query, new SqlParameter[] {
                new SqlParameter("@UserId", userId),
                new SqlParameter("@ModuleName", moduleName)
            });

            return (int)result > 0;
        }

        public List<Models.AppModule> GetUserPermissions(int userId)
        {
            string query = @"
                SELECT am.*, 
                       CASE WHEN up.UserId IS NOT NULL THEN 1 ELSE 0 END AS IsGranted
                FROM AppModules am
                LEFT JOIN UserPermissions up ON am.Id = up.ModuleId AND up.UserId = @UserId";

            var dt = _db.ExecuteQuery(query, new SqlParameter[] { new SqlParameter("@UserId", userId) });
            var modules = new List<Models.AppModule>();

            foreach (DataRow row in dt.Rows)
            {
                modules.Add(new Models.AppModule
                {
                    Id = (int)row["Id"],
                    ModuleName = row["ModuleName"].ToString(),
                    DisplayName = row["DisplayName"].ToString(),
                    IsGranted = (int)row["IsGranted"] == 1
                });
            }

            return modules;
        }

        public void UpdatePermissions(int userId, List<int> moduleIds)
        {
            // 1. Remove existing permissions
            _db.ExecuteNonQuery("DELETE FROM UserPermissions WHERE UserId = @UserId", 
                new SqlParameter[] { new SqlParameter("@UserId", userId) });

            // 2. Add new permissions
            foreach (var moduleId in moduleIds)
            {
                _db.ExecuteNonQuery("INSERT INTO UserPermissions (UserId, ModuleId) VALUES (@UserId, @ModuleId)", 
                    new SqlParameter[] { 
                        new SqlParameter("@UserId", userId), 
                        new SqlParameter("@ModuleId", moduleId) 
                    });
            }
        }
    }
}
