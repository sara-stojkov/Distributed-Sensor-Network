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

        public AlarmsController(IHubContext<AlarmHub> hubContext)
        {
            _hubContext = hubContext;
        }

        // IngestionService salje POST ovde kad detektuje alarm.
        [HttpPost]
        public async Task<IActionResult> PostAlarm([FromBody] AlarmMessage alarm)
        {
            // Prosledjujemo alarm svim konektovanim klijentima preko Hub-a.
            await _hubContext.Clients.All.SendAsync("ReceiveAlarm", alarm);

            return Ok();
        }
    }
}