using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using webclothes.Data;
using webclothes.Hubs;
using webclothes.Services;

var builder = WebApplication.CreateBuilder(args);

// 1. Cấu hình Database (Kết nối SQL Server)
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2. Cấu hình Identity chuẩn với Role (Quyền) và Default UI (Giao diện mặc định)
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options => {
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequiredLength = 6;
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders()
.AddDefaultUI();

// 3. Khai báo các dịch vụ (Services)
builder.Services.AddSession();
builder.Services.AddMemoryCache();
builder.Services.AddHttpContextAccessor();
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages(); // Bắt buộc phải có cho các trang đăng nhập/đăng ký
builder.Services.AddSignalR();
builder.Services.AddTransient<IEmailSender, EmailSender>();
builder.Services.AddScoped<webclothes.Services.VnPayService>();
builder.Services.AddScoped<webclothes.Services.INotificationService, webclothes.Services.NotificationService>();

var app = builder.Build();

// 4. Cấu hình Pipeline xử lý Request
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
var provider = new Microsoft.AspNetCore.StaticFiles.FileExtensionContentTypeProvider();
provider.Mappings[".avif"] = "image/avif";
provider.Mappings[".webp"] = "image/webp";

app.UseStaticFiles(new StaticFileOptions
{
    ContentTypeProvider = provider
});

app.UseRouting();

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

// 5. Khai báo định tuyến (Routing)
app.MapRazorPages(); // Kích hoạt đường dẫn cho Razor Pages (Identity)
app.MapHub<ChatHub>("/chatHub");
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");
app.MapControllerRoute(
    name: "product_seo",
    pattern: "san-pham/{id}",
    defaults: new { controller = "Products", action = "Details" });

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// 6. Tự động khởi tạo dữ liệu (Seeding Data) - Tạo tài khoản Admin
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await SchemaBootstrapper.EnsureLatestSchemaAsync(dbContext);

    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

    if (!await roleManager.RoleExistsAsync("Admin"))
    {
        await roleManager.CreateAsync(new IdentityRole("Admin"));
    }
    if (!await roleManager.RoleExistsAsync("Seller"))
    {
        await roleManager.CreateAsync(new IdentityRole("Seller"));
    }
    if (!await roleManager.RoleExistsAsync("Shipper"))
    {
        await roleManager.CreateAsync(new IdentityRole("Shipper"));
    }

    var adminEmail = "admin@store.com";
    if (await userManager.FindByEmailAsync(adminEmail) == null)
    {
        var adminUser = new IdentityUser { UserName = adminEmail, Email = adminEmail, EmailConfirmed = true };
        var result = await userManager.CreateAsync(adminUser, "Admin@123");
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(adminUser, "Admin");
        }
    }

    var sellerEmail = "seller@store.com";
    if (await userManager.FindByEmailAsync(sellerEmail) == null)
    {
        var sellerUser = new IdentityUser { UserName = sellerEmail, Email = sellerEmail, EmailConfirmed = true };
        var result = await userManager.CreateAsync(sellerUser, "Seller@123");
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(sellerUser, "Seller");
        }
    }

    var shipperEmail = "shipper@store.com";
    if (await userManager.FindByEmailAsync(shipperEmail) == null)
    {
        var shipperUser = new IdentityUser { UserName = shipperEmail, Email = shipperEmail, EmailConfirmed = true };
        var result = await userManager.CreateAsync(shipperUser, "Shipper@123");
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(shipperUser, "Shipper");
        }
    }

    // Seed Dummy Vouchers
    if (!dbContext.Vouchers.Any())
    {
        dbContext.Vouchers.AddRange(
            new webclothes.Models.Voucher { Code = "GIAM50K", DiscountAmount = 50000, Quantity = 100, ExpiryDate = DateTime.Now.AddMonths(1) },
            new webclothes.Models.Voucher { Code = "SALE20", DiscountAmount = 20000, Quantity = 50, ExpiryDate = DateTime.Now.AddMonths(1) }
        );
        dbContext.SaveChanges();
    }
}

app.Run();
