using ConsensusService.Data;
using ConsensusService.Models;
using Microsoft.EntityFrameworkCore;

namespace ConsensusService
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
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

            var groupedBySensor = eligibleReadings
                .GroupBy(r => r.SensorId)
                .ToDictionary(g => g.Key, g => g.ToList());

            _logger.LogInformation(
                "Consensus cycle: {SensorCount} sensors, {ReadingCount} readings eligible",
                groupedBySensor.Count, eligibleReadings.Count);

            // TODO: BFT calculation
        }
    }
}
