using Microsoft.AspNetCore.SignalR;

namespace NotificationService.Hubs
{
    // Klijenti (npr. konzolna app ili web stranica) se konektuju na ovaj hub
    // preko WebSocket-a i ostaju "pretplaceni" na alarm obavestenja.
    public class AlarmHub : Hub
    {
        // Ova metoda nije neophodna za sada, ali je korisna za testiranje:
        // klijent moze pozvati ovu metodu, hub odgovori svima.
        public async Task SendTestMessage(string message)
        {
            await Clients.All.SendAsync("ReceiveTestMessage", message);
        }
    }
}