using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using FocusFinderApp.Models;
using Microsoft.EntityFrameworkCore;

namespace FocusFinderApp.Controllers;

public class LocationController : Controller
{
    private readonly ILogger<LocationController> _logger;
    private readonly FocusFinderDbContext _dbContext;

    public LocationController(ILogger<LocationController> logger, FocusFinderDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
        Console.WriteLine("LocationController instantiated!");
    }

    [Route("/Locations")]
    public IActionResult Index()
    {
        ViewBag.IsLoggedIn = HttpContext.Session.GetInt32("UserId") != null;
        ViewBag.Username = HttpContext.Session.GetString("Username");

        var locations = _dbContext.Locations.Include(l => l.Reviews).ToList();

        if (ViewBag.IsLoggedIn)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            ViewBag.BookmarkedLocations = _dbContext.Bookmarks
                .Where(b => b.userId == userId)
                .Select(b => b.locationId)
                .ToList();
        }

        return View("~/Views/Home/Index.cshtml", locations);
    }

    [Route("/Locations/{id}")]
    [HttpGet]
    public IActionResult Location(int id)
    {
        if (id <= 0)
        {
            return RedirectToAction("Index");
        }
        var location = _dbContext.Locations
            // .Include(l => l.Reviews)
            .FirstOrDefault(l => l.Id == id);

        if (location == null)
        {
            Console.WriteLine("Location not found");
            return RedirectToAction("Index");
        }

        // Calculate average rating
        if (location.Reviews != null && location.Reviews.Any())
        {
            ViewBag.AverageRating = location.Reviews.Average(r => r.overallRating);
            ViewBag.AverageRating = Math.Round(ViewBag.AverageRating, 1);
        }
        else
        {
            ViewBag.AverageRating = "No ratings yet";
        }

        // Check if the user has already pressed Visited
        var locationIdForVisit = _dbContext.Locations.FirstOrDefault(l => l.Id == id);
        int? currentUserId = HttpContext.Session.GetInt32("UserId");
        ViewBag.IsLoggedIn = currentUserId;
        
        if (locationIdForVisit == null)
        {
            return NotFound();
        }

        var existingVisit = _dbContext.Visits.FirstOrDefault(l => l.locationId == id && l.userId == currentUserId);
        if (existingVisit != null)
        {
            Console.WriteLine("Visit already exists.");
            ViewBag.AlreadyVisited = "Already visited";     // << TBC!!
        }

        return View("~/Views/Home/Location.cshtml", location);
    }

    public IActionResult LocationByCity(string city)
    {
        if (city == null)
        {
            return RedirectToAction("Index");
        }
        var location = _dbContext.Locations
            .Where( l => l.City.ToLower() == city.ToLower())
            .ToList();
        
        if (location == null)
        {
            Console.WriteLine("Location not found");
            return RedirectToAction("Index");
        }
        return View("~/Views/Home/Index.cshtml", location);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    [HttpPost]
    public IActionResult AddReview(int LocationId, int Rating)
    {
        var location = _dbContext.Locations.FirstOrDefault(l => l.Id == LocationId);
        if (location == null)
        {
            return NotFound();
        }
        var newReview = new Review
        {
            locationId = LocationId,
            overallRating = Rating,
            dateLastUpdated = DateTime.UtcNow
        };
        _dbContext.Reviews.Add(newReview);
        _dbContext.SaveChanges();
        return RedirectToAction("Location", new { id = LocationId });
    }


    [HttpPost]
    public IActionResult AddVisit(int LocationId)
    {
        var location = _dbContext.Locations.FirstOrDefault(l => l.Id == LocationId);
        int? currentUserId = HttpContext.Session.GetInt32("UserId");
        if (location == null)
        {
            return NotFound();
        }

        // Check if the user has already pressed Visited
        var existingVisit = _dbContext.Visits.FirstOrDefault(l => l.locationId == LocationId && l.userId == currentUserId);

        if (existingVisit != null)
        {
            Console.WriteLine("Visit already exists.");
            ViewBag.AlreadyVisited = "Already visited";
        }
        else
        {
            // Add the Visit
            var newVisit = new Visit
        {
            locationId = LocationId,
            dateVisited = DateTime.UtcNow,
            userId = currentUserId
        };

            _dbContext.Visits.Add(newVisit);
            _dbContext.SaveChanges();

            ViewBag.AlreadyVisited = "Already visited";
        }

        return RedirectToAction("Location", new { id = LocationId });
    }

    [HttpPost]
    public IActionResult RemoveVisit(int LocationId)
    {
        var location = _dbContext.Locations.FirstOrDefault(l => l.Id == LocationId);
        int? currentUserId = HttpContext.Session.GetInt32("UserId");
        if (location == null)
        {
            return NotFound();
        }

        // Check if the user has already pressed Visited
        var existingVisit = _dbContext.Visits.FirstOrDefault(l => l.locationId == LocationId && l.userId == currentUserId);

        if (existingVisit == null)
        {
            Console.WriteLine("Not visted yet, so can't remove visit.");
            ViewBag.AlreadyVisited = "Not visited yet";  // << may change later
        }
        else
        {
            // Remove the Visit
            _dbContext.Visits.Remove(existingVisit);
            _dbContext.SaveChanges();

            ViewBag.AlreadyVisited = "Not visited yet";  // << may change later
        }

        return RedirectToAction("Location", new { id = LocationId });
    }
}
