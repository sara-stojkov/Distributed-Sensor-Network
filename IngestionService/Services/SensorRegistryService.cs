using IngestionService.Models;
using System.Collections.Concurrent;

namespace IngestionService.Services
{
    public class SensorRegistryService
    {
        private readonly ConcurrentDictionary<string, SensorStatus> _sensors = new();
        private readonly HttpClient _consensusClient;
        private readonly ILogger<SensorRegistryService> _logger;

        private static readonly TimeSpan InactiveThreshold = TimeSpan.FromSeconds(10);

        public SensorRegistryService(
            IConfiguration config,
            ILogger<SensorRegistryService> logger)
        {
            _logger = logger;
            string consensusUrl = config["Services:ConsensusService"]
                ?? "http://consensus-service:5051";
            _consensusClient = new HttpClient { BaseAddress = new Uri(consensusUrl) };
        }

        public void RecordHeartbeat(string sensorId)
        {
            _sensors.AddOrUpdate(
                sensorId,
                _ => new SensorStatus { SensorId = sensorId, LastSeen = DateTime.Now, IsActive = true },
                (_, existing) =>
                {
                    existing.LastSeen = DateTime.Now;
                    existing.IsActive = true;
                    return existing;
                });
        }

        public async Task StoreReadingAsync(SensorReading reading)
        {
            try
            {
                var content = new StringContent(
                    System.Text.Json.JsonSerializer.Serialize(reading),
                    System.Text.Encoding.UTF8,
                    "application/json");

                await _consensusClient.PostAsync("/api/consensus/reading", content);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Could not forward reading to ConsensusService: {msg}", ex.Message);
            }
        }

        public IEnumerable<SensorStatus> GetAllSensors()
        {
            foreach (var s in _sensors.Values)
                s.IsActive = (DateTime.Now - s.LastSeen) < InactiveThreshold;

            return _sensors.Values.OrderBy(s => s.SensorId);
        }

        public int GetActiveSensorCount()
            => _sensors.Values.Count(s => (DateTime.Now - s.LastSeen) < InactiveThreshold);
    }

    public class SensorStatus
    {
        public string SensorId { get; set; } = string.Empty;
        public DateTime LastSeen { get; set; }
        public bool IsActive { get; set; }
    }
}
