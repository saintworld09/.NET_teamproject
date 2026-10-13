
using EventHub.Data;
using EventHub.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<EventHubContext>(options =>
    options.UseSqlite(
        builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
    });

var app = builder.Build();

// Add starter event categories if the category table is empty.
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider
        .GetRequiredService<EventHubContext>();

    if (!context.Categories.Any())
    {
        context.Categories.AddRange(
            new Category
            {
                Name = "Technology",
                Description = "Technology conferences, workshops, and related events."
            },
            new Category
            {
                Name = "Business",
                Description = "Business, entrepreneurship, and professional development."
            },
            new Category
            {
                Name = "Education",
                Description = "Educational events, seminars, and learning activities."
            },
            new Category
            {
                Name = "Community",
                Description = "Community gatherings and social activities."
            }
        );

        context.SaveChanges();
    }
}

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

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<EventHub.Data.EventHubContext>();

    var eventsWithoutStatus = db.Events
        .Where(e => e.Status == "")
        .ToList();

    foreach (var eventItem in eventsWithoutStatus)
    {
        eventItem.Status = "Scheduled";
    }

    db.SaveChanges();
}


app.Run();