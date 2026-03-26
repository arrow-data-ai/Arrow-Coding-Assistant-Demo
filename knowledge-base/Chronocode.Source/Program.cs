using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Chronocode.Components;
using Chronocode.Components.Account;
using Chronocode.Data;
using Chronocode.Services;
// Allow internal access for unit testing
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Chronocode.Tests")]

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add controllers for API endpoints
builder.Services.AddControllers();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityUserAccessor>();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    })
    .AddIdentityCookies();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddIdentityCore<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddRoles<IdentityRole>()
    .AddRoleManager<RoleManager<IdentityRole>>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();

// Add Chronocode services
builder.Services.AddScoped<ProjectService>();
builder.Services.AddScoped<TaskService>();
builder.Services.AddScoped<ActivityService>();
builder.Services.AddScoped<ChargeCodeService>();
builder.Services.AddScoped<ReportService>();
builder.Services.AddScoped<AuthorizationService>();
builder.Services.AddScoped<HelpService>();
builder.Services.AddScoped<WorkAuthorizationArtifactService>();
builder.Services.AddScoped<EmailViewerService>();
builder.Services.AddSingleton<ChangelogService>();
builder.Services.AddSingleton<VersionService>();

var app = builder.Build();

// Ensure database is created and migrated on startup
await InitializeDatabaseAsync(app);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();


app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Map controller routes
app.MapControllers();

// Add additional endpoints required by the Identity /Account Razor components.
app.MapAdditionalIdentityEndpoints();

// Initialize roles and default admin user
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var authService = scope.ServiceProvider.GetRequiredService<AuthorizationService>();
    var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
    
    await AuthorizationService.EnsureRolesExistAsync(roleManager);
    await authService.EnsureDefaultAdminUserAsync(userManager, configuration);
}

await app.RunAsync();

static async Task InitializeDatabaseAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    try
    {
        // Get the connection string to determine database path
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        }
        
        // Ensure the Data directory exists
        var dataDirectory = Path.GetDirectoryName(connectionString);
        if (!string.IsNullOrEmpty(dataDirectory) && !Directory.Exists(dataDirectory))
        {
            Directory.CreateDirectory(dataDirectory);
            logger.LogInformation("Created database directory: {Directory}", dataDirectory);
        }
        
        // Check if database file exists
        var dbFileExists = File.Exists(connectionString);
        logger.LogInformation("Database file exists: {Exists} at path: {Path}", dbFileExists, connectionString);
        
        // Check if database exists and needs to be created
        var databaseExists = await context.Database.CanConnectAsync();
        
        if (!databaseExists)
        {
            logger.LogInformation("Database not accessible. Creating and migrating database...");
            await context.Database.MigrateAsync();
            logger.LogInformation("Database created and migrated successfully at: {DatabasePath}", connectionString);
        }
        else
        {
            // Apply any pending migrations
            var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
            if (pendingMigrations.Any())
            {
                logger.LogInformation("Applying {Count} pending migrations: {Migrations}", 
                    pendingMigrations.Count(), string.Join(", ", pendingMigrations));
                await context.Database.MigrateAsync();
                logger.LogInformation("Database migrations applied successfully.");
            }
            else
            {
                logger.LogInformation("Database is up to date.");
            }
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while initializing the database.");
        throw;
    }
}
