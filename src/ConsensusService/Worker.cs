using ConsensusService.Data;
using ConsensusService.Models;
using ConsensusService.Services;
using Microsoft.EntityFrameworkCore;

namespace ConsensusService
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly BftConsensusCalculator _calculator = new();
        private static readonly TimeSpan Interval = TimeSpan.FromSeconds(60);

        public Worker(ILogger<Worker> logger, IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await RunConsensusCycleAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Consensus cycle failed");
                }
                await Task.Delay(Interval, stoppingToken);
            }
        }

        private async Task RunConsensusCycleAsync(CancellationToken ct)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var cutoff = DateTime.UtcNow.AddMinutes(-1);

            var eligibleReadings = await db.SensorReadings
                .Where(r => r.Quality == SensorQuality.GOOD)
                .Where(r => !r.IsConsensus)
                .Where(r => r.Timestamp >= cutoff)
                .Where(r => r.Sensor!.Quality != SensorQuality.BAD)
                .ToListAsync(ct);

            if (eligibleReadings.Count == 0)
            {
                _logger.LogInformation("Consensus cycle: no eligible readings");
                return;
            }

            var groupedBySensor = eligibleReadings
                .GroupBy(r => r.SensorId)
                .ToDictionary(g => g.Key, g => g.ToList());

            _logger.LogInformation(
                "Consensus cycle: {SensorCount} sensors, {ReadingCount} readings eligible",
                groupedBySensor.Count, eligibleReadings.Count);

            // One vote per sensor: use each sensor's most recent reading this cycle.
            var votes = groupedBySensor
                .Select(kvp => new SensorVote(
                    kvp.Key,
                    kvp.Value.OrderByDescending(r => r.Timestamp).First().Value))
                .ToList();

            var result = _calculator.Calculate(votes);

            if (result.ConsensusValue is double consensusValue)
            {
                db.ConsensusValues.Add(new ConsensusValue
                {
                    Value = consensusValue,
                    Timestamp = DateTime.UtcNow,
                    ParticipatingSensors = result.ParticipatingSensors
                });
            }

            foreach (var outlierSensorId in result.OutlierSensorIds)
            {
                var sensor = await db.Sensors.FindAsync(new object[] { outlierSensorId }, ct);
                if (sensor != null)
                {
                    sensor.Quality = SensorQuality.BAD;
                    _logger.LogWarning("Sensor {SensorId} flagged as outlier, marked BAD", outlierSensorId);
                }
            }

            foreach (var reading in eligibleReadings)
            {
                reading.IsConsensus = true;
            }

            await db.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Consensus cycle complete: value={Value}, sensors={Count}, outliers={Outliers}",
                result.ConsensusValue, result.ParticipatingSensors,
                string.Join(",", result.OutlierSensorIds));
        }
    }
}