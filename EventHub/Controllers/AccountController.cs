using System.Security.Claims;
using EventHub.Data;
using EventHub.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventHub.Controllers;

public class AccountController : Controller
{
    private readonly EventHubContext _context;
    private readonly PasswordHasher<User> _passwordHasher;

    public AccountController(EventHubContext context)
    {
        _context = context;
        _passwordHasher = new PasswordHasher<User>();
    }

    // =========================
    // LOGIN
    // =========================

    [HttpGet]
    public IActionResult Login()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(string email, string password)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            ModelState.AddModelError(
                "email",
                "Email address is required.");
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            ModelState.AddModelError(
                "password",
                "Password is required.");
        }

        if (!ModelState.IsValid)
        {
            return View();
        }

        email = email.Trim().ToLower();

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == email);

        if (user == null)
        {
            ModelState.AddModelError(
                string.Empty,
                "Invalid email address or password.");

            return View();
        }

        var passwordResult = _passwordHasher.VerifyHashedPassword(
            user,
            user.PasswordHash,
            password);

        if (passwordResult == PasswordVerificationResult.Failed)
        {
            ModelState.AddModelError(
                string.Empty,
                "Invalid email address or password.");

            return View();
        }

        var claims = new List<Claim>
        {
            new Claim(
                ClaimTypes.NameIdentifier,
                user.UserId.ToString()),

            new Claim(
                ClaimTypes.Name,
                $"{user.FirstName} {user.LastName}"),

            new Claim(
                ClaimTypes.Email,
                user.Email),

            new Claim(
                ClaimTypes.Role,
                user.Role)
        };

        var claimsIdentity = new ClaimsIdentity(
            claims,
            CookieAuthenticationDefaults.AuthenticationScheme);

        var claimsPrincipal = new ClaimsPrincipal(
            claimsIdentity);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            claimsPrincipal);

        return RedirectToAction(
            "Index",
            "Dashboard");
    }

    // =========================
    // LOGOUT
    // =========================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(
            CookieAuthenticationDefaults.AuthenticationScheme);

        return RedirectToAction(
            "Index",
            "Home");
    }

    // =========================
    // ATTENDEE REGISTRATION
    // =========================

    [HttpGet]
    public IActionResult Register()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(
        string firstName,
        string lastName,
        string email,
        string password,
        string confirmPassword)
    {
        if (string.IsNullOrWhiteSpace(firstName))
        {
            ModelState.AddModelError(
                "firstName",
                "First name is required.");
        }

        if (string.IsNullOrWhiteSpace(lastName))
        {
            ModelState.AddModelError(
                "lastName",
                "Last name is required.");
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            ModelState.AddModelError(
                "email",
                "Email address is required.");
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            ModelState.AddModelError(
                "password",
                "Password is required.");
        }

        if (password != confirmPassword)
        {
            ModelState.AddModelError(
                "confirmPassword",
                "Passwords do not match.");
        }

        if (!string.IsNullOrWhiteSpace(email))
        {
            email = email.Trim().ToLower();

            bool emailExists = await _context.Users
                .AnyAsync(u => u.Email == email);

            if (emailExists)
            {
                ModelState.AddModelError(
                    "email",
                    "An account with this email already exists.");
            }
        }

        if (!ModelState.IsValid)
        {
            return View();
        }

        var user = new User
        {
            FirstName = firstName.Trim(),
            LastName = lastName.Trim(),
            Email = email,
            Role = "Attendee"
        };

        user.PasswordHash = _passwordHasher.HashPassword(
            user,
            password);

        _context.Users.Add(user);

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] =
            "Your attendee account has been created successfully. Please log in.";

        return RedirectToAction(nameof(Login));
    }

    // =========================
    // ORGANIZER REGISTRATION
    // =========================

    [HttpGet("/Account/Organizer/Register")]
    public IActionResult OrganizerRegister()
    {
        return View("Organizer/Register");
    }

    [HttpPost("/Account/Organizer/Register")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> OrganizerRegister(
        string firstName,
        string lastName,
        string email,
        string password,
        string confirmPassword)
    {
        if (string.IsNullOrWhiteSpace(firstName))
        {
            ModelState.AddModelError(
                "firstName",
                "First name is required.");
        }

        if (string.IsNullOrWhiteSpace(lastName))
        {
            ModelState.AddModelError(
                "lastName",
                "Last name is required.");
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            ModelState.AddModelError(
                "email",
                "Email address is required.");
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            ModelState.AddModelError(
                "password",
                "Password is required.");
        }

        if (password != confirmPassword)
        {
            ModelState.AddModelError(
                "confirmPassword",
                "Passwords do not match.");
        }

        if (!string.IsNullOrWhiteSpace(email))
        {
            email = email.Trim().ToLower();

            bool emailExists = await _context.Users
                .AnyAsync(u => u.Email == email);

            if (emailExists)
            {
                ModelState.AddModelError(
                    "email",
                    "An account with this email already exists.");
            }
        }

        if (!ModelState.IsValid)
        {
            return View("Organizer/Register");
        }

        var user = new User
        {
            FirstName = firstName.Trim(),
            LastName = lastName.Trim(),
            Email = email,
            Role = "Organizer"
        };

        user.PasswordHash = _passwordHasher.HashPassword(
            user,
            password);

        _context.Users.Add(user);

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] =
            "Your organizer account has been created successfully. Please log in.";

        return RedirectToAction(nameof(Login));
    }

    // =========================
    // ACCESS DENIED
    // =========================

    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }
}