var builder = WebApplication.CreateBuilder(args);



// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddScoped<OfficeSuite.Data.SqlHelper>();
builder.Services.AddScoped<OfficeSuite.Services.FileHelper>(provider => 
    new OfficeSuite.Services.FileHelper(builder.Environment.WebRootPath));
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<OfficeSuite.Services.PermissionService>();
builder.Services.AddScoped<OfficeSuite.Services.NotificationService>();
builder.Services.AddSingleton<OfficeSuite.Services.EmailService>();
builder.Services.AddHostedService<OfficeSuite.Services.ReminderEmailWorker>();
builder.Services.AddAuthentication("CookieAuth")
    .AddCookie("CookieAuth", options =>
    {
        options.Cookie.Name = "OfficeSuite.Auth";
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(24);
        options.SlidingExpiration = true;
    });

var app = builder.Build();

// Run DB Initialization
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<OfficeSuite.Data.SqlHelper>();
    OfficeSuite.Data.DbInitializer.Initialize(db);
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/Home/StatusCode", "?code={0}");

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

app.Run();
