using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using OfficeSuite.Data;
using OfficeSuite.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace OfficeSuite.Services
{
    public class ReminderEmailWorker : BackgroundService
    {
        private readonly ILogger<ReminderEmailWorker> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _config;

        public ReminderEmailWorker(ILogger<ReminderEmailWorker> logger, IServiceProvider serviceProvider, IConfiguration config)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _config = config;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Reminder Email Worker is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessReminders();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while processing reminders.");
                }

                // Wait for 1 hour before next check
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }

            _logger.LogInformation("Reminder Email Worker is stopping.");
        }

        private async Task ProcessReminders()
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<SqlHelper>();
                var emailService = scope.ServiceProvider.GetRequiredService<EmailService>();
                var adminEmail = _config["EmailSettings:AdminEmail"] ?? "thehypenx@gmail.com";

                // Fetch reminders that are due (ReminderDate <= Now) and not sent and not deleted
                string query = "SELECT * FROM Reminders WHERE ReminderDate <= GETDATE() AND IsSent = 0 AND (IsDeleted = 0 OR IsDeleted IS NULL)";
                var dt = db.ExecuteQuery(query);

                foreach (DataRow row in dt.Rows)
                {
                    var reminderId = (int)row["Id"];
                    var clientName = row["ClientName"]?.ToString() ?? "";
                    var message = row["Message"]?.ToString() ?? "";
                    var reminderDate = (DateTime)row["ReminderDate"];

                    _logger.LogInformation($"Sending reminder email for ID {reminderId} to {adminEmail}");

                    string subject = $"Invoice Reminder: {clientName}";
                    string body = $@"
                        <h3>Invoice Reminder</h3>
                        <p><strong>Client:</strong> {clientName}</p>
                        <p><strong>Reminder:</strong> {message}</p>
                        <p><strong>Due Date:</strong> {reminderDate:f}</p>
                        <br/>
                        <p>Please check the HypenX CRM for more details.</p>";

                    try 
                    {
                        await emailService.SendEmailAsync(adminEmail, subject, body);

                        // Mark as sent
                        db.ExecuteNonQuery("UPDATE Reminders SET IsSent = 1 WHERE Id = @Id", new SqlParameter[] {
                            new SqlParameter("@Id", reminderId)
                        });

                        _logger.LogInformation($"Successfully sent and updated reminder ID {reminderId}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Failed to send email for reminder ID {reminderId}");
                    }
                }
            }
        }
    }
}
