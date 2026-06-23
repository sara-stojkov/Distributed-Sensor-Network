using ConsensusService.Data;
using ConsensusService.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ConsensusService.Controllers;

[ApiController]
[Route("api/reports")]
public class ReportsController : ControllerBase
{
    private readonly AppDbContext _db;

    public ReportsController(AppDbContext db)
    {
        _db = db;
    }

    // GET /api/reports/consensus?from=2026-06-01&to=2026-06-30
    [HttpGet("consensus")]
    public async Task<IActionResult> GetConsensusValues(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken ct)
    {
        var query = _db.ConsensusValues.AsQueryable();

        if (from.HasValue)
            query = query.Where(c => c.Timestamp >= from.Value.ToUniversalTime());

        if (to.HasValue)
            query = query.Where(c => c.Timestamp <= to.Value.ToUniversalTime());

        var results = await query
            .OrderByDescending(c => c.Timestamp)
            .ToListAsync(ct);

        return Ok(results);
    }

    // GET /api/reports/readings?sensorId=sensor-1&from=2026-06-01
    [HttpGet("readings")]
    public async Task<IActionResult> GetReadings(
        [FromQuery] string? sensorId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken ct)
    {
        var query = _db.SensorReadings.AsQueryable();

        if (!string.IsNullOrEmpty(sensorId))
            query = query.Where(r => r.SensorId == sensorId);

        if (from.HasValue)
            query = query.Where(r => r.Timestamp >= from.Value.ToUniversalTime());

        if (to.HasValue)
            query = query.Where(r => r.Timestamp <= to.Value.ToUniversalTime());

        var results = await query
            .OrderByDescending(r => r.Timestamp)
            .Take(500) // safety limit — don't return the entire table
            .ToListAsync(ct);

        return Ok(results);
    }

    // GET /api/reports/sensors — list all known sensors and their current status
    [HttpGet("sensors")]
    public async Task<IActionResult> GetSensors(CancellationToken ct)
    {
        var sensors = await _db.Sensors
            .OrderBy(s => s.Id)
            .ToListAsync(ct);

        return Ok(sensors);
    }
}