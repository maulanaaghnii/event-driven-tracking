using Microsoft.AspNetCore.Mvc;
using System.IO;

namespace vehicle_tracker_v2.Controllers
{
    public class GeoJsonController : Controller
    {
        private readonly IWebHostEnvironment _environment;

        public GeoJsonController(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        [HttpGet("api/geojson")]
        public IActionResult GetGeoJson()
        {
            var filePath = Path.Combine(_environment.WebRootPath, "converted.geojson");
            
            if (!System.IO.File.Exists(filePath))
            {
                return NotFound();
            }

            var jsonContent = System.IO.File.ReadAllText(filePath);
            return Content(jsonContent, "application/json");
        }
    }
} 