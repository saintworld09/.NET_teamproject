using System.Security.Claims;
using EventHub.Data;
using EventHub.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventHub.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly EventHubContext _context;

    public DashboardController(EventHubContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        if (!int.TryParse(
                User.FindFirstValue(ClaimTypes.NameIdentifier),
                out int userId))
        {
            return Forbid();
        }

        // Use UTC because PostgreSQL timestamp with time zone
        // expects UTC DateTime values when queried through Npgsql.
        DateTime now = DateTime.UtcNow;

        // Statistics and recent events for an Organizer.
        if (User.IsInRole("Organizer"))
        {
            var myEvents = _context.Events
                .Where(e => e.OrganizerId == userId);

            ViewBag.TotalEvents = await myEvents.CountAsync();

            ViewBag.ScheduledEvents = await myEvents
                .CountAsync(e => e.Status == "Scheduled");

            ViewBag.CancelledEvents = await myEvents
                .CountAsync(e => e.Status == "Cancelled");

            ViewBag.TotalRegistrations = await _context.Registrations
                .CountAsync(r =>
                    r.Event != null &&
                    r.Event.OrganizerId == userId &&
                    r.Status == "Registered");

            ViewBag.RecentEvents = await myEvents
                .AsNoTracking()
                .Include(e => e.Category)
                .OrderByDescending(e => e.CreatedAt)
                .Take(5)
                .ToListAsync();
        }
        // Statistics and recent events for an Attendee.
        else if (User.IsInRole("Attendee"))
        {
            var myRegistrations = _context.Registrations
                .Where(r => r.UserId == userId);

            ViewBag.RegisteredEvents = await myRegistrations
                .CountAsync(r => r.Status == "Registered");

            ViewBag.UpcomingEvents = await myRegistrations
                .CountAsync(r =>
                    r.Status == "Registered" &&
                    r.Event != null &&
                    r.Event.Status == "Scheduled" &&
                    r.Event.EventDate >= now);

            ViewBag.PastEvents = await myRegistrations
                .CountAsync(r =>
                    r.Status == "Registered" &&
                    r.Event != null &&
                    r.Event.EventDate < now);

            ViewBag.CancelledRegistrations = await myRegistrations
                .CountAsync(r => r.Status == "Cancelled");

            ViewBag.RecentEvents = await myRegistrations
                .Where(r =>
                    r.Status == "Registered" &&
                    r.Event != null)
                .AsNoTracking()
                .Include(r => r.Event)
                    .ThenInclude(e => e!.Category)
                .OrderByDescending(r => r.RegisteredAt)
                .Select(r => r.Event!)
                .Take(5)
                .ToListAsync();
        }

        return View();
    }
}