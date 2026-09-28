using Microsoft.AspNetCore.Mvc;

namespace EventHub.Controllers;

public class EventsController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}