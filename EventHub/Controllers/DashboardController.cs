using Microsoft.AspNetCore.Mvc;

namespace EventHub.Controllers;

public class DashboardController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}