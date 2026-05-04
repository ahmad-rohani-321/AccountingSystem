using AccountingSystem.Data;
using AccountingSystem.Models.Identity;
using AccountingSystem.Repository.Inventory;
using DevExpress.AspNetCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

var connectionString = new SqliteConnectionStringBuilder
{
    DataSource = "AccountingSystemDatabase.db",
    Mode = SqliteOpenMode.ReadWriteCreate,
    Password = "AccountingSystem@123#123.Ahmad@Rohani"
};

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString.ConnectionString)
    .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning)));

builder.Services.AddIdentity<User, IdentityRole>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// for devexpress
builder.Services.AddDevExpressControls();


builder.Services
    .AddControllersWithViews()
    .AddJsonOptions(options => options.JsonSerializerOptions.PropertyNamingPolicy = null);

builder.Services.ConfigureApplicationCookie(p =>
{
    p.LoginPath = "/auth";
}
    );

// Localization (server-side)
// var supportedCultures = new[] { new CultureInfo("ps-AF") };
// CultureInfo.DefaultThreadCurrentCulture = supportedCultures[0];
// CultureInfo.DefaultThreadCurrentUICulture = supportedCultures[0];
// var requestLocalizationOptions = new RequestLocalizationOptions
// {
//     DefaultRequestCulture = new RequestCulture("ps-AF"),
//     SupportedCultures = supportedCultures,
//     SupportedUICultures = supportedCultures
// };
// requestLocalizationOptions.RequestCultureProviders = new IRequestCultureProvider[]
// {
//     new CookieRequestCultureProvider(),
//     new AcceptLanguageHeaderRequestCultureProvider()
// };

#region Injections
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<IUnitRepository, UnitRepository>();
builder.Services.AddScoped<IWarehouseRepository, WarehouseRepository>();
builder.Services.AddScoped<IItemsRepository, ItemsRepository>();
#endregion

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}
app.UseStaticFiles();

//app.UseRequestLocalization(requestLocalizationOptions);

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// for devexpress
app.UseDevExpressControls();

app.MapControllers();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
