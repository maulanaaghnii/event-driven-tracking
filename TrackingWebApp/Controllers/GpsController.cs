using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;
using System.Text.Json;

namespace vehicle_tracker_new.Controllers
{
    [ApiController]
    [Route("api/gps")]
    public class GpsController : ControllerBase
    {
        private readonly IConnectionMultiplexer _redis;
        public GpsController(IConnectionMultiplexer redis)
        {
            _redis = redis;
        }

        [HttpGet("{unitno}")]
        public IActionResult GetGps(string unitno)
        {
            var db = _redis.GetDatabase();
            var value = db.StringGet($"unit:{unitno}");
            if (value.IsNullOrEmpty)
            {
                return NotFound(new { error = "Data not found" });
            }
            // Data sudah dalam bentuk JSON string
            return Content(value, "application/json");
        }
    }
} 