
using Cutly.API.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Cutly.API.Controllers
{
    [ApiController]
    [Route("api/health")]
    public class HealthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public HealthController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("database")]
        public async Task<IActionResult> CheckDatabase()
        {
            try
            {
                var canConnect = await _context.Database.CanConnectAsync();

                if (!canConnect)
                {
                    return StatusCode(503, new
                    {
                        status = "error",
                        message = "Cannot connect to PostgreSQL."
                    });
                }

                return Ok(new
                {
                    status = "healthy",
                    database = "PostgreSQL",
                    message = "Database connection successful."
                });
            }
            catch (Exception)
            {
                return StatusCode(503, new
                {
                    status = "error",
                    message = "Database connection failed."
                });
            }
        }
    }
}
