using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using System.Text.Json;

// ── Konfiguracija ──────────────────────────────────────────────────────────
// Lokalno (dotnet run, bez Docker/K8s):
const string HubUrl = "https://localhost:7241/hubs/alarms";

// Kroz Ingress (kad je Minikube + tunnel pokrenut):
// const string HubUrl = "http://localhost/hubs/alarms";

// Demo na dva laptopa (zameni IP sa pravim IP-om servera):
// const string HubUrl = "http://192.168.1.15/hubs/alarms";
// ─────────────────────────────────────────────────────────────────────────

Console.WriteLine("=== NotificationService Alarm Client ===");
Console.WriteLine($"Konektovanje na: {HubUrl}\n");

var connection = new HubConnectionBuilder()
    .WithUrl(HubUrl, options =>
    {
        // Dozvoljava self-signed dev sertifikat lokalno.
        // NE koristiti u produkciji - ovde je ok jer je interna demo mreža.
        options.HttpMessageHandlerFactory = _ => new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback =
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };
    })
    .WithAutomaticReconnect()
    .ConfigureLogging(logging =>
    {
        // Bez ovoga, izuzeci u handlerima (npr. RuntimeBinderException kod dynamic)
        // se tiho gutaju i ne vidis ih na konzoli - zato je delovalo kao da
        // "ne stizu alarmi", a u stvari je handler pucao na prvoj liniji.
        logging.AddConsole();
        logging.SetMinimumLevel(LogLevel.Warning);
    })
    .Build();

// VAZNO: server salje JSON preko System.Text.Json. Kad se to deserijalizuje u
// `object`/`dynamic`, runtime tip NIJE ExpandoObject (kao u JS-u) nego JsonElement -
// a JsonElement nema property-je tipa "sensorId", samo metode kao GetProperty(...).
// Pristup alarm.sensorId na takvom dynamic-u baca RuntimeBinderException odmah,
// pa handler nikad ne stigne do Console.WriteLine. Zato koristimo JsonElement
// eksplicitno, sa GetProperty/TryGetProperty - isto kao sto HTML klijent radi
// prirodno nad plain JS objektom.
connection.On<JsonElement>("ReceiveAlarm", (alarm) =>
{
    int priority = GetIntProperty(alarm, "alarmPriority")
                ?? GetIntProperty(alarm, "priority")
                ?? 0;

    var color = priority switch
    {
        1 => ConsoleColor.Yellow,
        2 => ConsoleColor.DarkYellow,
        3 => ConsoleColor.Red,
        _ => ConsoleColor.White
    };

    string sensorId = GetStringProperty(alarm, "sensorId") ?? "N/A";
    string temperature = GetRawProperty(alarm, "temperature") ?? GetRawProperty(alarm, "value") ?? "N/A";
    string quality = GetStringProperty(alarm, "quality") ?? "N/A";

    var prevColor = Console.ForegroundColor;
    Console.ForegroundColor = color;
    Console.WriteLine(
        $"[{DateTime.Now:HH:mm:ss}] *** ALARM P{priority} *** " +
        $"Sensor={sensorId}  Temp={temperature}°C  Quality={quality}");
    Console.ForegroundColor = prevColor;
});

// Helperi za bezbedno citanje vrednosti iz JsonElement-a (case-insensitive po default-u u SignalR-u).
static int? GetIntProperty(JsonElement el, string name) =>
    el.ValueKind == JsonValueKind.Object && el.TryGetProperty(name, out var p) && p.ValueKind != JsonValueKind.Null
        ? p.GetInt32()
        : (int?)null;

static string? GetStringProperty(JsonElement el, string name) =>
    el.ValueKind == JsonValueKind.Object && el.TryGetProperty(name, out var p) && p.ValueKind != JsonValueKind.Null
        ? p.ToString()
        : null;

static string? GetRawProperty(JsonElement el, string name) =>
    el.ValueKind == JsonValueKind.Object && el.TryGetProperty(name, out var p) && p.ValueKind != JsonValueKind.Null
        ? p.ToString()
        : null;

connection.Closed += async (error) =>
{
    Console.WriteLine($"[Diskonektovan] {error?.Message}");
    await Task.Delay(2000);
};

connection.Reconnecting += (error) =>
{
    Console.WriteLine("[Ponovno povezivanje...]");
    return Task.CompletedTask;
};

connection.Reconnected += (connectionId) =>
{
    Console.WriteLine("[Ponovo konektovan]");
    return Task.CompletedTask;
};

try
{
    await connection.StartAsync();
    Console.WriteLine($"Konektovan. ConnectionId={connection.ConnectionId}, State={connection.State}");
    Console.WriteLine("Čekam alarme... (Ctrl+C za izlaz)\n");
}
catch (Exception ex)
{
    Console.WriteLine($"Greška pri konektovanju: {ex.Message}");
    return;
}

// Drži app živom dok čeka alarme.
await Task.Delay(Timeout.Infinite);