using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using NotificationService.Hubs;
using NotificationService.Models;

namespace NotificationService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AlarmsController : ControllerBase
    {
        private readonly IHubContext<AlarmHub> _hubContext;
        private readonly ILogger<AlarmsController> _logger;

        public AlarmsController(IHubContext<AlarmHub> hubContext, ILogger<AlarmsController> logger)
        {
            _hubContext = hubContext;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> PostAlarm([FromBody] AlarmMessage alarm)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(
                $"[HUB-IN ] SensorId={alarm.SensorId}  Temperature={alarm.Temperature}  " +
                $"AlarmPriority={alarm.AlarmPriority}  Quality={alarm.Quality}  Timestamp={alarm.Timestamp:O}");
            Console.ResetColor();

            var outgoingJson = System.Text.Json.JsonSerializer.Serialize(alarm);
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine($"[HUB-OUT] Broadcast 'ReceiveAlarm' payload: {outgoingJson}");
            Console.ResetColor();

            _logger.LogInformation("Alarm received from IngestionService: {Json}", outgoingJson);

            await _hubContext.Clients.All.SendAsync("ReceiveAlarm", alarm);

            return Ok();
        }
    }
}