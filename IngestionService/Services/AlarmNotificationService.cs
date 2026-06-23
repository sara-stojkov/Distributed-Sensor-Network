using IngestionService.Models;

namespace IngestionService.Services
{
    public class AlarmNotificationService
    {
        private readonly HttpClient _notificationClient;
        private readonly ILogger<AlarmNotificationService> _logger;

        public AlarmNotificationService(
            IConfiguration config,
            ILogger<AlarmNotificationService> logger)
        {
            _logger = logger;
            string notificationUrl = config["Services:NotificationService"]
                ?? "http://notification-service:5052";
            _notificationClient = new HttpClient { BaseAddress = new Uri(notificationUrl) };
        }


        public async Task NotifyAlarmAsync(SensorReading reading)
        {
            PrintAlarmToConsole(reading);

            try
            {
                var payload = new
                {
                    sensorId = reading.SensorId,
                    temperature = reading.Temperature,
                    alarmPriority = reading.AlarmPriority,
                    timestamp = reading.Timestamp,
                    quality = reading.Quality.ToString()
                };

                var content = new StringContent(
                    System.Text.Json.JsonSerializer.Serialize(payload),
                    System.Text.Encoding.UTF8,
                    "application/json");

                await _notificationClient.PostAsync("/api/notifications/alarm", content);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Could not forward alarm to NotificationService: {msg}", ex.Message);
            }
        }


        private static void PrintAlarmToConsole(SensorReading reading)
        {
            ConsoleColor color = reading.AlarmPriority switch
            {
                1 => ConsoleColor.Yellow,
                2 => ConsoleColor.DarkYellow,
                3 => ConsoleColor.Red,
                _ => ConsoleColor.White
            };

            lock (Console.Out)
            {
                var prev = Console.ForegroundColor;
                Console.ForegroundColor = color;
                Console.WriteLine(
                    $"[{DateTime.Now:HH:mm:ss}] *** ALARM P{reading.AlarmPriority} ***  " +
                    $"Sensor: {reading.SensorId}  T={reading.Temperature:F2}°C");
                Console.ForegroundColor = prev;
            }
        }
    }
}
