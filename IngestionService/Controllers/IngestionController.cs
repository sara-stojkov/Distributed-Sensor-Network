using IngestionService.Models;
using IngestionService.Services;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;

namespace IngestionService.Controllers
{
    [ApiController]
    [Route("api/ingest")]
    public class IngestionController : ControllerBase
    {
        private readonly RSA _serverRsa;
        private readonly SensorRegistryService _registry;
        private readonly AlarmNotificationService _alarms;
        private readonly ReplayProtectionService _replay;

        public IngestionController(
            RSA serverRsa,
            SensorRegistryService registry,
            AlarmNotificationService alarms,
            ReplayProtectionService replay)
        {
            _serverRsa = serverRsa;
            _registry = registry;
            _alarms = alarms;
            _replay = replay;
        }


        [HttpPost("reading")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public async Task<IActionResult> PostReading([FromBody] SecureMessage message)
        {
            if (!DateTime.TryParse(message.SentAt, out var sentAt))
                return BadRequest("Invalid SentAt timestamp.");

            if (Math.Abs((DateTime.Now - sentAt).TotalSeconds) > 30)
                return Conflict("Message timestamp outside acceptable window (replay attack suspected).");

            if (!_replay.Accept(message.SensorId, message.MessageId))
                return Conflict($"Duplicate or out-of-order MessageId {message.MessageId} for sensor {message.SensorId}.");

            var reading = CryptoService.Decrypt(message, _serverRsa);
            if (reading == null)
                return BadRequest("Message decryption or signature verification failed.");

            _registry.RecordHeartbeat(reading.SensorId);

            LogReading(reading);

            if (reading.AlarmPriority > 0)
                await _alarms.NotifyAlarmAsync(reading);

            await _registry.StoreReadingAsync(reading);

            return Ok(new { received = reading.SensorId, timestamp = reading.Timestamp });
        }

        private static void LogReading(SensorReading reading)
        {
            ConsoleColor color = reading.AlarmPriority switch
            {
                1 => ConsoleColor.Yellow,
                2 => ConsoleColor.DarkYellow,
                3 => ConsoleColor.Red,
                _ => ConsoleColor.Gray
            };

            string alarmStr = reading.AlarmPriority == 0
                ? ""
                : $"  ALARM PRIORITY {reading.AlarmPriority}";

            lock (Console.Out)
            {
                var prev = Console.ForegroundColor;
                Console.ForegroundColor = color;
                Console.WriteLine(
                    $"[{DateTime.Now:HH:mm:ss.fff}] [SERVER] [{reading.SensorId}] " +
                    $"T={reading.Temperature:F2}°C  Q={reading.Quality}  MSG#{reading.MessageId}{alarmStr}");
                Console.ForegroundColor = prev;
            }
        }
    }

    [ApiController]
    [Route("api/ingest")]
    public class SensorStatusController : ControllerBase
    {
        private readonly SensorRegistryService _registry;

        public SensorStatusController(SensorRegistryService registry)
            => _registry = registry;


        [HttpGet("sensors")]
        public IActionResult GetSensors()
            => Ok(_registry.GetAllSensors());


        [HttpGet("sensors/active-count")]
        public IActionResult GetActiveCount()
            => Ok(new { activeCount = _registry.GetActiveSensorCount() });
    }
}