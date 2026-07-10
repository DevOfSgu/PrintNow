using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Net.payOS;
using PrintNow.Web.Data;
using PrintNow.Web.Models;
using PrintNow.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddSignalR();
builder.Services.AddSingleton<ActiveUserTracker>();
builder.Services.AddSingleton<OnlineUserTracker>();

builder.Services.AddDbContext<PrintNowContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";
        options.LogoutPath = "/Auth/Logout";
        options.AccessDeniedPath = "/Auth/AccessDenied";
    });

// Cấu hình PayOS
var payOSClientId = builder.Configuration["PayOS:ClientId"] ?? "";
var payOSApiKey = builder.Configuration["PayOS:ApiKey"] ?? "";
var payOSChecksumKey = builder.Configuration["PayOS:ChecksumKey"] ?? "";

if (!string.IsNullOrEmpty(payOSClientId) && !string.IsNullOrEmpty(payOSApiKey) && !string.IsNullOrEmpty(payOSChecksumKey))
{
    var payOS = new PayOS(payOSClientId, payOSApiKey, payOSChecksumKey);
    builder.Services.AddSingleton(payOS);
}
else
{
    // Sẽ kiểm tra null trong controller và thông báo cần cấu hình
    builder.Services.AddSingleton<PayOS?>(sp => (PayOS?)null);
}

var app = builder.Build();

// Seed admin account
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<PrintNowContext>();
    context.Database.EnsureCreated();

    if (!context.Users.Any(u => u.Role == "Admin"))
    {
        var adminUser = new User
        {
            FullName = "Admin PrintNow",
            Phone = "0900000000",
            Email = "admin@printnow.vn",
            PasswordHash = "admin123",
            Role = "Admin",
            CreatedAt = DateTime.UtcNow
        };
        context.Users.Add(adminUser);
        context.SaveChanges();
    }
}

// Ép kiểu định dạng số theo chuẩn quốc tế (tránh lỗi dấu phẩy/chấm)
var cultureInfo = new System.Globalization.CultureInfo("en-US");
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture(cultureInfo),
    SupportedCultures = new List<System.Globalization.CultureInfo> { cultureInfo },
    SupportedUICultures = new List<System.Globalization.CultureInfo> { cultureInfo }
});

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Middleware theo dõi người dùng đang online
app.Use(async (context, next) =>
{
    if (context.User.Identity?.IsAuthenticated == true)
    {
        var userIdClaim = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(userIdClaim, out var userId))
        {
            var tracker = context.RequestServices.GetRequiredService<ActiveUserTracker>();
            tracker.TrackUser(userId);
        }
    }
    await next();
});

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapHub<PrintNow.Web.Hubs.ChatHub>("/chatHub");
app.MapHub<PrintNow.Web.Hubs.UserTrackingHub>("/userTrackingHub");

app.Run();
