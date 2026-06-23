using IngestionService.Data;
using IngestionService.Models;
using Microsoft.EntityFrameworkCore;

namespace IngestionService.Services
{
    public class ReadingPersistenceService
    {
        private readonly IDbContextFactory<IngestionDbContext> _dbFactory;
        private readonly ILogger<ReadingPersistenceService> _logger;

        public ReadingPersistenceService(
            IDbContextFactory<IngestionDbContext> dbFactory,
            ILogger<ReadingPersistenceService> logger)
        {
            _dbFactory = dbFactory;
            _logger = logger;
        }

        public async Task PersistAsync(SensorReading reading)
        {
            try
            {
                await using var db = await _dbFactory.CreateDbContextAsync();

                var sensor = await db.Sensors.FindAsync(reading.SensorId);

                if (sensor == null)
                {
                    sensor = new SensorEntity
                    {
                        Id = reading.SensorId,
                        MinRange = reading.MinRange,
                        MaxRange = reading.MaxRange,
                        Quality = MapQuality(reading.Quality),
                        AlarmThreshold1 = reading.AlarmThreshold1,
                        AlarmThreshold2 = reading.AlarmThreshold2,
                        AlarmThreshold3 = reading.AlarmThreshold3,
                        LastMessageAt = reading.Timestamp
                    };
                    db.Sensors.Add(sensor);

                    await db.SaveChangesAsync();
                    _logger.LogInformation("Registered new sensor {Id} in DB.", reading.SensorId);
                }
                else
                {
                    sensor.LastMessageAt = reading.Timestamp;
                    sensor.Quality = MapQuality(reading.Quality);
                    db.Sensors.Update(sensor);
                }

                var entity = new SensorReadingEntity
                {
                    SensorId = reading.SensorId,
                    Value = reading.Temperature,
                    Timestamp = DateTime.SpecifyKind(reading.Timestamp, DateTimeKind.Utc),
                    Quality = MapQuality(reading.Quality),
                    AlarmPriority = reading.AlarmPriority,
                    IsConsensus = false
                };
                db.SensorReadings.Add(entity);

                await db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to persist reading for sensor {Id}.", reading.SensorId);
            }
        }

        private static SensorQualityDb MapQuality(DataQuality q) => q switch
        {
            DataQuality.Good => SensorQualityDb.GOOD,
            DataQuality.Bad => SensorQualityDb.BAD,
            DataQuality.Uncertain => SensorQualityDb.UNCERTAIN,
            _ => SensorQualityDb.GOOD
        };
    }
}
