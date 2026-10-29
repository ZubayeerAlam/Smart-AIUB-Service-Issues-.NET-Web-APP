using Microsoft.EntityFrameworkCore;
using Smart_AIUB_Service_Issue.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// The connection string lives in User Secrets (development), not in appsettings.json.
var connectionString = builder.Configuration.GetConnectionString("AIUB_Service&IssuesDbContext")
    ?? throw new InvalidOperationException(
        "Connection string 'AIUB_Service&IssuesDbContext' not found. " +
        "Set it with: dotnet user-secrets set \"ConnectionStrings:AIUB_Service&IssuesDbContext\" \"<connection string>\"");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// Shows a helpful page for database/migration errors (Development only).
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
