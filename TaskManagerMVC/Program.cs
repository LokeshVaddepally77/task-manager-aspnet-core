using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.EntityFrameworkCore;
using TaskManagerMVC.DataAccess.Data;
using TaskManagerMVC.DataAccess.Repository;
using TaskManagerMVC.DataAccess.Services;
using AuthService = TaskManagerMVC.DataAccess.Services.AuthenticationService;
using IAuthService = TaskManagerMVC.DataAccess.Services.IAuthenticationService;
using IPasswordHelper = TaskManagerMVC.DataAccess.Services.IPasswordHelper;
using PasswordHelper = TaskManagerMVC.DataAccess.Services.PasswordHelper;


var builder = WebApplication.CreateBuilder(args);

// ==================== ADD SERVICES ====================

builder.Services.AddControllersWithViews();

// ==================== DATABASE CONFIGURATION ====================

var dbProvider = builder.Configuration["DatabaseProvider"] ?? "MySQL";

switch (dbProvider)
{
    case "InMemory":
        builder.Services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase("TaskManagerDb")
                   .EnableSensitiveDataLogging());
        break;

    case "SQLServer":
        var sqlConnection = builder.Configuration.GetConnectionString("SQLServerConnection");
        builder.Services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(sqlConnection)
                   .EnableSensitiveDataLogging());
        break;

    case "MySQL":
    default:
        var mysqlConnection = builder.Configuration.GetConnectionString("MySQLConnection");
        var serverVersion = new MySqlServerVersion(new Version(8, 0, 25));
        builder.Services.AddDbContext<AppDbContext>(options =>
            options.UseMySql(mysqlConnection, serverVersion)
                   .EnableSensitiveDataLogging()
                   .LogTo(Console.WriteLine, LogLevel.Information));
        break;
}

// ==================== REGISTER SERVICES ====================

// Repository
builder.Services.AddScoped<ITaskRepository, SQLTaskRepository>();

// Authentication Services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IPasswordHelper, PasswordHelper>();

builder.Services.AddScoped<IRoleService, RoleService>();

// ==================== AUTHENTICATION CONFIGURATION ====================

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
})
.AddCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    options.SlidingExpiration = true;
    options.Cookie.Name = ".TaskManager.Auth";
})
.AddGoogle(googleOptions =>
{
    // Read from User Secrets or appsettings.json
    googleOptions.ClientId = builder.Configuration["Authentication:Google:ClientId"]
        ?? throw new InvalidOperationException("Google ClientId is not configured");
    googleOptions.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"]
        ?? throw new InvalidOperationException("Google ClientSecret is not configured");

    googleOptions.SaveTokens = true;

    // Request user profile information
    googleOptions.Scope.Add("profile");
    googleOptions.Scope.Add("email");

    // Map claims for easy access
    //googleOptions.ClaimActions.MapJsonKey("picture", "picture");
    //googleOptions.ClaimActions.MapJsonKey("email_verified", "email_verified");
});

// ==================== SESSION CONFIGURATION ====================

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Name = ".TaskManager.Session";
});

builder.Services.AddHttpContextAccessor();

// ==================== BUILD APPLICATION ====================

var app = builder.Build();

// ==================== DATABASE INITIALIZATION ====================

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var passwordHelper = scope.ServiceProvider.GetRequiredService<IPasswordHelper>();

    if (dbProvider != "InMemory")
    {
        // Apply pending migrations
        //dbContext.Database.Migrate();
        dbContext.Database.EnsureCreated();
    }
    else
    {
        dbContext.Database.EnsureCreated();
    }

    // Initialize user passwords
    InitializeUserPasswords(dbContext, passwordHelper);
}

// ==================== PASSWORD INITIALIZATION ====================

void InitializeUserPasswords(AppDbContext context, IPasswordHelper passwordHelper)
{
    var users = context.Users.ToList();
    var needsSave = false;

    foreach (var user in users)
    {
        // Check if password is placeholder or invalid
        if (string.IsNullOrEmpty(user.PasswordHash) ||
            user.PasswordHash == "PLACEHOLDER_WILL_BE_SET_ON_STARTUP" ||
            !user.PasswordHash.Contains(";"))
        {
            // Set default password: password123
            user.PasswordHash = passwordHelper.HashPassword("password123");
            needsSave = true;
            Console.WriteLine($"Password initialized for user: {user.Username}");
        }
    }

    if (needsSave)
    {
        context.SaveChanges();
        Console.WriteLine("User passwords have been initialized successfully.");
    }
}

// ==================== CONFIGURE HTTP PIPELINE ====================

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// IMPORTANT: Order matters!
// 1. Session
// 2. Authentication
// 3. Authorization
app.UseSession();
app.UseAuthentication();  // ← NEW: Must come before UseAuthorization
app.UseAuthorization();

// ==================== ROUTE CONFIGURATION ====================

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

app.Run();