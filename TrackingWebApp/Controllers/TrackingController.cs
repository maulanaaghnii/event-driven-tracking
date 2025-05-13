using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using TrackingWebApp.Models;

namespace TrackingWebApp.Controllers;

public class TrackingController : Controller
{
    private readonly ILogger<TrackingController> _logger;

    public TrackingController(ILogger<TrackingController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
