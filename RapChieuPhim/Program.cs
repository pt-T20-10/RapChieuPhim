using RapChieuPhim.Data;
using Microsoft.EntityFrameworkCore;
using Hangfire;
using Hangfire.SqlServer;
using RapChieuPhim.Services;
using PayOS;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(
        builder.Configuration.GetValue<int>("AppSettings:SessionTimeoutMinutes"));
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});
builder.Services.AddHttpContextAccessor();

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
})
.AddCookie()
.AddGoogle(GoogleDefaults.AuthenticationScheme, options =>
{
    // LƯU Ý: Bạn phải thay 2 dòng này bằng Key lấy từ Google Cloud Console nhé!
    options.ClientId = "xxx";
    options.ClientSecret = "xxx";
});


builder.Services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSqlServerStorage(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddHangfireServer();

builder.Services.AddControllersWithViews();

// Đăng ký PayOS
PayOSClient payOSClient = new PayOSClient(
    builder.Configuration["PayOS:ClientId"] ?? "",
    builder.Configuration["PayOS:ApiKey"] ?? "",
    builder.Configuration["PayOS:ChecksumKey"] ?? ""
);
builder.Services.AddSingleton(payOSClient);

builder.Services.AddRazorPages();

// Service registrations
builder.Services.AddScoped<DatVeService>();
builder.Services.AddScoped<ThongKeService>();
builder.Services.AddScoped<DichVuervice>();
builder.Services.AddScoped<AccountService>();
builder.Services.AddScoped<QRCodeService>();
builder.Services.AddScoped<QuetVeService>();
builder.Services.AddScoped<PdfTicketService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/NguoiDung/Home/Error");
    app.UseHsts();
}

// ❌ Tạm comment lại dòng này khi test
// app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseHangfireDashboard(builder.Configuration["Hangfire:DashboardPath"]);

// ── Razor Pages Routes ─────────────
app.MapRazorPages();

// ── Area Routes — Controllers ─────────────
app.MapAreaControllerRoute(
    name: "RapPhim",
    areaName: "RapPhim",
    pattern: "RapPhim/{controller=Dashboard}/{action=Index}/{id?}");
app.MapAreaControllerRoute(
    name: "RapPhim",
    areaName: "RapPhim",
    pattern: "RapPhim/{controller=Dashboard}/{action=Index}/{id?}");
app.MapAreaControllerRoute(
    name: "NguoiDung",
    areaName: "NguoiDung",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();