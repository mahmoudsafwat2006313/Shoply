using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Shoply.Data;
using Shoply.Models;
using Shoply.Data;
using Shoply.Models;
using Stripe;

var builder = WebApplication.CreateBuilder(args);

// ✅ إعداد قاعدة البيانات (SQLite)
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// ✅ إضافة Identity مع Roles
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>();

// ✅ إضافة MVC و Razor Pages
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

// ✅ تفعيل الجلسات
builder.Services.AddSession();

// ✅ تمكين HttpContextAccessor (مهم للجلسة)
builder.Services.AddHttpContextAccessor();

// ✅ إعداد Stripe
StripeConfiguration.ApiKey = builder.Configuration["Stripe:SecretKey"];

var app = builder.Build();

// ✅ إنشاء قاعدة البيانات تلقائيًا + استيراد المنتجات
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.EnsureCreated();

    // استدعاء الـ DataSeeder لإضافة المنتجات
    DataSeeder.SeedProducts(db);

    // ✅ إنشاء دور Admin وحساب أدمن افتراضي
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

    if (!await roleManager.RoleExistsAsync("Admin"))
        await roleManager.CreateAsync(new IdentityRole("Admin"));

    string adminEmail = "admin@shopverse.com";
    string adminPass = "Admin@123";

    var adminUser = await userManager.FindByEmailAsync(adminEmail);
    if (adminUser == null)
    {
        adminUser = new ApplicationUser { UserName = adminEmail, Email = adminEmail };
        await userManager.CreateAsync(adminUser, adminPass);
        await userManager.AddToRoleAsync(adminUser, "Admin");
    }
}

// ✅ إعدادات البيئة
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();

// ➤ ترتيب مهم: Authentication قبل Authorization
app.UseAuthentication();
app.UseAuthorization();

// ✅ مسار لوحة التحكم (Admin Area)
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

// ✅ المسار الافتراضي (للمستخدم العادي)
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.Logger.LogInformation("✅ Shopverse is running in {EnvironmentName}", app.Environment.EnvironmentName);

app.Run();