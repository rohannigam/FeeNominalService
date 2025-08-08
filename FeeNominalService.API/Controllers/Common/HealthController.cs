using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using FeeNominalService.Data;
using System.Diagnostics;
using System.Reflection;
using Microsoft.AspNetCore.Authorization;

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
        [AllowAnonymous]
        public IActionResult Liveness()
        {
            var informationalVersion = Assembly.GetExecutingAssembly()
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
            var fileVersion = Assembly.GetExecutingAssembly()
                .GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version;
            return Ok(new { status = "Alive", service = "FeeNominalService", version = informationalVersion, buildVersion = fileVersion });
        }

        // Readiness probe - checks critical dependencies (DB)
        [HttpGet("ready")]
        [AllowAnonymous]
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
        [AllowAnonymous]
        public IActionResult Health()
        {
            return Ok(new { status = "Alive", service = "FeeNominalService" });
        }
    }
} 