using StudentBazaar.Web.Hubs;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using StudentBazaar.DataAccess;
using StudentBazaar.Entities.Models;

var cultureInfo = new CultureInfo("en-EG");
CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;
var builder = WebApplication.CreateBuilder(args);

// ==============================
// 1- Database Connection
// ==============================
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("No Connection String was Found");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// ==============================
// 2- Identity (Users + Roles)
// ==============================
builder.Services.AddIdentity<ApplicationUser, IdentityRole<int>>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;

    options.Password.RequiredLength = 6;
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// ==============================
// 3- MVC Controllers + Views + Razor Pages
// ==============================
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

// ==============================
// 4- Repositories
// ==============================

builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<ICollegeRepository, CollegeRepository>();
builder.Services.AddScoped<IListingRepository, ListingRepository>();
builder.Services.AddScoped<IMajorRepository, MajorRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IProductCategoryRepository, ProductCategoryRepository>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IRatingRepository, RatingRepository>();
builder.Services.AddScoped<IShoppingCartItemRepository, ShoppingCartItemRepository>();
builder.Services.AddScoped<IUniversityRepository, UniversityRepository>();

builder.Services.AddSignalR();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<StudentBazaar.Web.Services.IActivityLogService, StudentBazaar.Web.Services.ActivityLogService>();
builder.Services.AddScoped<StudentBazaar.Web.Services.INotificationService, StudentBazaar.Web.Services.NotificationService>();

var app = builder.Build();

// ==============================
// 5- Middleware
// ==============================
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// ==============================
// 8- Check Blocked Users Middleware
// ==============================
app.Use(async (context, next) =>
{
    // تخطي التحقق لصفحة Login نفسها لتجنب loop
    if (context.Request.Path.StartsWithSegments("/Account/Login"))
    {
        await next();
        return;
    }
    
    if (context.User?.Identity?.IsAuthenticated == true)
    {
        using (var scope = app.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var userId = userManager.GetUserId(context.User);
            
            if (!string.IsNullOrEmpty(userId) && int.TryParse(userId, out int userIdInt))
            {
                var user = await userManager.FindByIdAsync(userId);
                if (user != null && user.IsBlocked)
                {
                    // تسجيل الخروج تلقائياً
                    var signInManager = scope.ServiceProvider.GetRequiredService<SignInManager<ApplicationUser>>();
                    await signInManager.SignOutAsync();
                    
                    // إعادة توجيه إلى صفحة Login مع رسالة
                    context.Response.Redirect($"/Account/Login?blocked=true&reason={Uri.EscapeDataString(user.BlockReason ?? "Violation of terms and conditions")}");
                    return;
                }
            }
        }
    }
    
    await next();
});

// ==============================
// 6- Routing
// ==============================

// Area routing
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

// 🔥 أول صفحة → صفحة المنتجات
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Product}/{action=Index}/{id?}");

app.MapHub<ChatHub>("/chathub");
app.MapHub<AdminHub>("/adminhub");

app.MapRazorPages();

// ==============================
// 7- Apply Migrations & Seed Roles + Admin User
// ==============================
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    
    // Apply pending migrations automatically
    try
    {
        context.Database.Migrate();
    }
    catch (Exception ex)
    {
        // If migration fails, try to add columns manually via SQL
    }
    
    // Always ensure required columns exist (fallback for manual SQL execution)
    try
    {
        var sql = @"
            IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('AspNetUsers') AND name = 'IsBlocked')
                ALTER TABLE AspNetUsers ADD IsBlocked BIT NOT NULL DEFAULT 0;
            IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('AspNetUsers') AND name = 'BlockReason')
                ALTER TABLE AspNetUsers ADD BlockReason NVARCHAR(500) NULL;
            IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('AspNetUsers') AND name = 'BlockedAt')
                ALTER TABLE AspNetUsers ADD BlockedAt DATETIME2 NULL;
            IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('AspNetUsers') AND name = 'BlockedByUserId')
                ALTER TABLE AspNetUsers ADD BlockedByUserId INT NULL;
            IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Products') AND name = 'IsForRent')
                ALTER TABLE Products ADD IsForRent BIT NOT NULL DEFAULT 0;
            IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Products') AND name = 'PricePerDay')
                ALTER TABLE Products ADD PricePerDay DECIMAL(18, 2) NULL;
            IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('ChatMessages') AND name = 'DeletedBySender')
                ALTER TABLE ChatMessages ADD DeletedBySender BIT NOT NULL DEFAULT 0;
            IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('ChatMessages') AND name = 'DeletedByReceiver')
                ALTER TABLE ChatMessages ADD DeletedByReceiver BIT NOT NULL DEFAULT 0;
        ";
        context.Database.ExecuteSqlRaw(sql);
    }
    catch
    {
        // Ignore if columns already exist or if table doesn't exist yet
    }
    
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<int>>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

    // -------- Create Roles --------
    string[] roles = { "Student", "Admin" };

    foreach (var role in roles)
    {
        if (!roleManager.RoleExistsAsync(role).GetAwaiter().GetResult())
        {
            roleManager.CreateAsync(new IdentityRole<int>(role)).GetAwaiter().GetResult();
        }
    }

    // -------- Create Admin User --------
    string adminEmail = "admin@admin.com";
    string adminPassword = "Admin123!";

    var adminUser = userManager.FindByEmailAsync(adminEmail).GetAwaiter().GetResult();

    if (adminUser == null)
    {
        adminUser = new ApplicationUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            FullName = "Super Admin",
            EmailConfirmed = true
        };

        var create = userManager.CreateAsync(adminUser, adminPassword).GetAwaiter().GetResult();

        if (create.Succeeded)
        {
            userManager.AddToRoleAsync(adminUser, "Admin").GetAwaiter().GetResult();
        }
        else
        {
            // optional logging
        }
    }
}

app.Run();
