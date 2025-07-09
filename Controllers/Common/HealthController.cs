using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using FeeNominalService.Data;
using System.Diagnostics;

namespace FeeNominalService.Controllers.Common
{
    [ApiController]
    [Route("api/health")]
    public class HealthController : ControllerBase
    {
        private readonly ApplicationDbContext _dbContext;

        public HealthController(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        // Liveness probe - lightweight, no dependencies
        [HttpGet("live")]
        public IActionResult Liveness()
        {
            return Ok(new { status = "Alive", service = "FeeNominalService" });
        }

        // Readiness probe - checks critical dependencies (DB)
        [HttpGet("ready")]
        public async Task<IActionResult> Readiness()
        {
            var sw = Stopwatch.StartNew();
            string? version = null;
            try
            {
                using var command = _dbContext.Database.GetDbConnection().CreateCommand();
                command.CommandText = "SELECT version()";
                await _dbContext.Database.OpenConnectionAsync();
                version = (await command.ExecuteScalarAsync())?.ToString();

                // Simple query to check DB connectivity
                await _dbContext.Database.ExecuteSqlRawAsync("SELECT 1");
                sw.Stop();
                return Ok(new { status = "Ready", db = "PostgreSQL", rds = true, version, latencyMs = sw.ElapsedMilliseconds });
            }
            catch (Exception ex)
            {
                sw.Stop();
                return StatusCode(503, new { status = "NotReady", db = "PostgreSQL", rds = true, version, latencyMs = sw.ElapsedMilliseconds, error = ex.Message });
            }
        }

        // Legacy endpoint for backward compatibility
        [HttpGet]
        public IActionResult Health()
        {
            return Ok(new { status = "Alive", service = "FeeNominalService" });
        }
    }
} 