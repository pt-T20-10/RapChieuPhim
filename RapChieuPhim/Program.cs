using RapChieuPhim.Data;
using Microsoft.EntityFrameworkCore;
using Hangfire;
using Hangfire.SqlServer;
using RapChieuPhim.Services;

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


builder.Services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSqlServerStorage(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddHangfireServer();

builder.Services.AddControllersWithViews();
builder.Services.AddScoped<DatVeService>();
builder.Services.AddScoped<AccountService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/NguoiDung/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseHangfireDashboard(builder.Configuration["Hangfire:DashboardPath"]);

// ── Area Routes — dùng MapAreaControllerRoute ─────────────
app.MapAreaControllerRoute(
    name: "RapPhim",
    areaName: "RapPhim",
    pattern: "RapPhim/{controller=Dashboard}/{action=Index}/{id?}");

app.MapAreaControllerRoute(
    name: "NguoiDung",
    areaName: "NguoiDung",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();