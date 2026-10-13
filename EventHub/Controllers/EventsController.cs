
using System.Security.Claims;
using EventHub.Data;
using EventHub.Models;
using EventHub.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace EventHub.Controllers;

public class EventsController : Controller
{
    private readonly EventHubContext _context;

    public EventsController(EventHubContext context)
    {
        _context = context;
    }

    // Anyone can browse events.
    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var events = await _context.Events
            .Include(e => e.Category)
            .Include(e => e.Organizer)
            .Include(e => e.Registrations)
            .OrderBy(e => e.EventDate)
            .ToListAsync();

        return View(events);
    }

    // Only organizers can open the Create Event form.
    [Authorize(Roles = "Organizer")]
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var viewModel = new CreateEventViewModel();

        await LoadCategoriesAsync(viewModel);

        return View(viewModel);
    }

    // Only organizers can submit the Create Event form.
    [Authorize(Roles = "Organizer")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        CreateEventViewModel viewModel)
    {
        if (viewModel.EventDate.HasValue &&
            viewModel.EventDate.Value <= DateTime.Now)
        {
            ModelState.AddModelError(
                nameof(viewModel.EventDate),
                "The event date and time must be in the future.");
        }

        bool categoryExists = await _context.Categories
            .AnyAsync(c => c.CategoryId == viewModel.CategoryId);

        if (!categoryExists)
        {
            ModelState.AddModelError(
                nameof(viewModel.CategoryId),
                "Please select a valid event category.");
        }

        if (!ModelState.IsValid)
        {
            await LoadCategoriesAsync(viewModel);
            return View(viewModel);
        }

        var userIdClaim = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (!int.TryParse(userIdClaim, out int organizerId))
        {
            return Forbid();
        }

        bool organizerExists = await _context.Users
            .AnyAsync(u => u.UserId == organizerId &&
                           u.Role == "Organizer");

        if (!organizerExists)
        {
            return Forbid();
        }

        var newEvent = new Event
        {
            Title = viewModel.Title.Trim(),
            Description = viewModel.Description.Trim(),
            EventDate = viewModel.EventDate!.Value,
            Location = viewModel.Location.Trim(),
            Capacity = viewModel.Capacity,
            CategoryId = viewModel.CategoryId,
            OrganizerId = organizerId,
            CreatedAt = DateTime.UtcNow,
            Status = "Scheduled"
        };

        _context.Events.Add(newEvent);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] =
            "Your event was created successfully.";

        return RedirectToAction(nameof(Index));
    }

    // Display the full details of an event.
    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var eventItem = await _context.Events
            .Include(e => e.Category)
            .Include(e => e.Organizer)
            .Include(e => e.Registrations)
            .FirstOrDefaultAsync(e => e.EventId == id.Value);

        if (eventItem == null)
        {
            return NotFound();
        }

        int registeredCount = eventItem.Registrations
            .Count(r => r.Status == "Registered");

        bool isRegistered = false;
        int? registrationId = null;

        var userIdClaim = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (int.TryParse(userIdClaim, out int userId))
        {
            var activeRegistration = eventItem.Registrations
                .Where(r => r.UserId == userId &&
                            r.Status == "Registered")
                .OrderByDescending(r => r.RegisteredAt)
                .FirstOrDefault();

            isRegistered = activeRegistration != null;
            registrationId = activeRegistration?.RegistrationId;
        }

        ViewBag.RegisteredCount = registeredCount;
        ViewBag.IsRegistered = isRegistered;
        ViewBag.RegistrationId = registrationId;

        return View(eventItem);
    }

    // Only attendees can register for events.
    [Authorize(Roles = "Attendee")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(int id)
    {
        var userIdClaim = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (!int.TryParse(userIdClaim, out int userId))
        {
            return Forbid();
        }

        await using var transaction =
            await _context.Database.BeginTransactionAsync();

        try
        {
            var eventItem = await _context.Events
                .Include(e => e.Registrations)
                .FirstOrDefaultAsync(e => e.EventId == id);

            if (eventItem == null)
            {
                await transaction.RollbackAsync();
                return NotFound();
            }

            if (eventItem.Status == "Cancelled")
            {
                await transaction.RollbackAsync();

                TempData["ErrorMessage"] =
                    "Registration is unavailable because this event has been cancelled.";

                return RedirectToAction(nameof(Details), new { id });
            }

            if (eventItem.EventDate <= DateTime.Now)
            {
                await transaction.RollbackAsync();

                TempData["ErrorMessage"] =
                    "Registration is closed because this event has already started or passed.";

                return RedirectToAction(nameof(Details), new { id });
            }

            bool alreadyRegistered = eventItem.Registrations.Any(
                r => r.UserId == userId &&
                     r.Status == "Registered");

            if (alreadyRegistered)
            {
                await transaction.RollbackAsync();

                TempData["ErrorMessage"] =
                    "You are already registered for this event.";

                return RedirectToAction(nameof(Details), new { id });
            }

            int registeredCount = eventItem.Registrations.Count(
                r => r.Status == "Registered");

            if (registeredCount >= eventItem.Capacity)
            {
                await transaction.RollbackAsync();

                TempData["ErrorMessage"] =
                    "Sorry, this event has reached its maximum capacity.";

                return RedirectToAction(nameof(Details), new { id });
            }

            var registration = new Registration
            {
                UserId = userId,
                EventId = eventItem.EventId,
                RegisteredAt = DateTime.UtcNow,
                Status = "Registered"
            };

            _context.Registrations.Add(registration);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            TempData["SuccessMessage"] =
                "You have successfully registered for this event.";

            return RedirectToAction(nameof(Details), new { id });
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync();

            TempData["ErrorMessage"] =
                "Your registration could not be completed. You may already be registered, or the event may have reached capacity.";

            return RedirectToAction(nameof(Details), new { id });
        }
    }

    // Attendees can cancel their own active registration.
    // The registration record is preserved with Status = "Cancelled".
    [Authorize(Roles = "Attendee")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelMyRegistration(int id)
    {
        var userIdClaim = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (!int.TryParse(userIdClaim, out int userId))
        {
            return Forbid();
        }

        var registration = await _context.Registrations
            .Include(r => r.Event)
            .FirstOrDefaultAsync(r =>
                r.RegistrationId == id &&
                r.UserId == userId);

        if (registration == null || registration.Event == null)
        {
            return NotFound();
        }

        if (registration.Status != "Registered")
        {
            TempData["ErrorMessage"] =
                "This registration is no longer active.";

            return RedirectToAction(nameof(MyEvents));
        }

        registration.Status = "Cancelled";

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] =
            "Your event registration has been cancelled successfully.";

        return RedirectToAction(nameof(MyEvents));
    }

    // Organizers can cancel registrations for events they own.
    [Authorize(Roles = "Organizer")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelRegistration(int id)
    {
        var userIdClaim = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (!int.TryParse(userIdClaim, out int organizerId))
        {
            return Forbid();
        }

        var registration = await _context.Registrations
            .Include(r => r.Event)
            .FirstOrDefaultAsync(r =>
                r.RegistrationId == id);

        if (registration == null || registration.Event == null)
        {
            return NotFound();
        }

        if (registration.Event.OrganizerId != organizerId)
        {
            return Forbid();
        }

        if (registration.Status != "Registered")
        {
            TempData["ErrorMessage"] =
                "This registration is no longer active.";

            return RedirectToAction(
                nameof(Attendees),
                new { id = registration.EventId });
        }

        registration.Status = "Cancelled";

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] =
            "The attendee's registration was cancelled successfully.";

        return RedirectToAction(
            nameof(Attendees),
            new { id = registration.EventId });
    }

    // Organizers see their own events.
    // Attendees see events in their registration history.
    [Authorize(Roles = "Organizer,Attendee")]
    [HttpGet]
    public async Task<IActionResult> MyEvents()
    {
        var userIdClaim = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (!int.TryParse(userIdClaim, out int userId))
        {
            return Forbid();
        }

        if (User.IsInRole("Organizer"))
        {
            var organizerEvents = await _context.Events
                .Where(e => e.OrganizerId == userId)
                .Include(e => e.Category)
                .Include(e => e.Registrations)
                .OrderBy(e => e.EventDate)
                .ToListAsync();

            ViewBag.UserRole = "Organizer";
            ViewBag.CurrentUserId = userId;

            return View(organizerEvents);
        }

        if (User.IsInRole("Attendee"))
        {
            var registeredEventIds = await _context.Registrations
                .Where(r => r.UserId == userId)
                .Select(r => r.EventId)
                .Distinct()
                .ToListAsync();

            var attendeeEvents = await _context.Events
                .Where(e => registeredEventIds.Contains(e.EventId))
                .Include(e => e.Category)
                .Include(e => e.Registrations)
                .OrderBy(e => e.EventDate)
                .ToListAsync();

            ViewBag.UserRole = "Attendee";
            ViewBag.CurrentUserId = userId;

            return View(attendeeEvents);
        }

        return Forbid();
    }

    // Organizers can view attendees for events they own.
    [Authorize(Roles = "Organizer")]
    [HttpGet]
    public async Task<IActionResult> Attendees(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var userIdClaim = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (!int.TryParse(userIdClaim, out int organizerId))
        {
            return Forbid();
        }

        var eventItem = await _context.Events
            .Include(e => e.Registrations)
                .ThenInclude(r => r.User)
            .FirstOrDefaultAsync(e => e.EventId == id.Value);

        if (eventItem == null)
        {
            return NotFound();
        }

        if (eventItem.OrganizerId != organizerId)
        {
            return Forbid();
        }

        ViewBag.RegisteredCount = eventItem.Registrations
            .Count(r => r.Status == "Registered");

        return View(eventItem);
    }

    // Display the edit form for an event owned by this organizer.
    [Authorize(Roles = "Organizer")]
    [HttpGet]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var userIdClaim = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (!int.TryParse(userIdClaim, out int organizerId))
        {
            return Forbid();
        }

        var eventItem = await _context.Events
            .FirstOrDefaultAsync(e => e.EventId == id.Value);

        if (eventItem == null)
        {
            return NotFound();
        }

        if (eventItem.OrganizerId != organizerId)
        {
            return Forbid();
        }

        if (eventItem.Status == "Cancelled")
        {
            TempData["ErrorMessage"] =
                "Cancelled events cannot be edited.";

            return RedirectToAction(nameof(MyEvents));
        }

        var viewModel = new CreateEventViewModel
        {
            Title = eventItem.Title,
            Description = eventItem.Description,
            EventDate = eventItem.EventDate,
            Location = eventItem.Location,
            Capacity = eventItem.Capacity,
            CategoryId = eventItem.CategoryId
        };

        await LoadCategoriesAsync(viewModel);

        return View(viewModel);
    }

    // Save changes to an event owned by this organizer.
    [Authorize(Roles = "Organizer")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int id,
        CreateEventViewModel viewModel)
    {
        var userIdClaim = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (!int.TryParse(userIdClaim, out int organizerId))
        {
            return Forbid();
        }

        var eventItem = await _context.Events
            .FirstOrDefaultAsync(e => e.EventId == id);

        if (eventItem == null)
        {
            return NotFound();
        }

        if (eventItem.OrganizerId != organizerId)
        {
            return Forbid();
        }

        if (eventItem.Status == "Cancelled")
        {
            TempData["ErrorMessage"] =
                "Cancelled events cannot be edited.";

            return RedirectToAction(nameof(MyEvents));
        }

        if (viewModel.EventDate.HasValue &&
            viewModel.EventDate.Value <= DateTime.Now)
        {
            ModelState.AddModelError(
                nameof(viewModel.EventDate),
                "The event date and time must be in the future.");
        }

        bool categoryExists = await _context.Categories
            .AnyAsync(c => c.CategoryId == viewModel.CategoryId);

        if (!categoryExists)
        {
            ModelState.AddModelError(
                nameof(viewModel.CategoryId),
                "Please select a valid event category.");
        }

        if (!ModelState.IsValid)
        {
            await LoadCategoriesAsync(viewModel);
            return View(viewModel);
        }

        eventItem.Title = viewModel.Title.Trim();
        eventItem.Description = viewModel.Description.Trim();
        eventItem.EventDate = viewModel.EventDate!.Value;
        eventItem.Location = viewModel.Location.Trim();
        eventItem.Capacity = viewModel.Capacity;
        eventItem.CategoryId = viewModel.CategoryId;

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] =
            "Your event was updated successfully.";

        return RedirectToAction(nameof(MyEvents));
    }

    // Display the delete confirmation page.
    [Authorize(Roles = "Organizer")]
    [HttpGet]
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var userIdClaim = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (!int.TryParse(userIdClaim, out int organizerId))
        {
            return Forbid();
        }

        var eventItem = await _context.Events
            .Include(e => e.Category)
            .Include(e => e.Registrations)
            .FirstOrDefaultAsync(e => e.EventId == id.Value);

        if (eventItem == null)
        {
            return NotFound();
        }

        if (eventItem.OrganizerId != organizerId)
        {
            return Forbid();
        }

        return View(eventItem);
    }

    // Delete only events that have no registration records.
    [Authorize(Roles = "Organizer")]
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var userIdClaim = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (!int.TryParse(userIdClaim, out int organizerId))
        {
            return Forbid();
        }

        var eventItem = await _context.Events
            .Include(e => e.Registrations)
            .FirstOrDefaultAsync(e => e.EventId == id);

        if (eventItem == null)
        {
            return NotFound();
        }

        if (eventItem.OrganizerId != organizerId)
        {
            return Forbid();
        }

        if (eventItem.Registrations.Any())
        {
            TempData["ErrorMessage"] =
                "This event cannot be deleted because it has registrations. You can cancel it instead.";

            return RedirectToAction(nameof(MyEvents));
        }

        _context.Events.Remove(eventItem);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] =
            "Event deleted successfully.";

        return RedirectToAction(nameof(MyEvents));
    }

    // Cancel an event without deleting its registration records.
    [Authorize(Roles = "Organizer")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        var userIdClaim = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (!int.TryParse(userIdClaim, out int organizerId))
        {
            return Forbid();
        }

        var eventItem = await _context.Events
            .FirstOrDefaultAsync(e => e.EventId == id);

        if (eventItem == null)
        {
            return NotFound();
        }

        if (eventItem.OrganizerId != organizerId)
        {
            return Forbid();
        }

        if (eventItem.Status == "Cancelled")
        {
            TempData["ErrorMessage"] =
                "This event has already been cancelled.";

            return RedirectToAction(nameof(MyEvents));
        }

        if (eventItem.EventDate <= DateTime.Now)
        {
            TempData["ErrorMessage"] =
                "An event that has already started or passed cannot be cancelled here.";

            return RedirectToAction(nameof(MyEvents));
        }

        eventItem.Status = "Cancelled";

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] =
            "Event cancelled successfully. Existing registrations have been preserved.";

        return RedirectToAction(nameof(MyEvents));
    }

    // Load categories for the Create and Edit forms.
    private async Task LoadCategoriesAsync(
        CreateEventViewModel viewModel)
    {
        viewModel.Categories = await _context.Categories
            .OrderBy(c => c.Name)
            .Select(c => new SelectListItem
            {
                Value = c.CategoryId.ToString(),
                Text = c.Name
            })
            .ToListAsync();
    }
}
