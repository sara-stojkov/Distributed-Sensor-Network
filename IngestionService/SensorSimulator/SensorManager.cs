using IngestionService.Models;
using System.Security.Cryptography;

namespace SensorSimulator
{
    public class SensorManager
    {
        private readonly List<SensorNode> _allSensors = [];
        private readonly List<Task> _sensorTasks = [];
        private readonly RSA _serverPublicKey;
        private readonly HttpClient _http;
        private readonly CancellationTokenSource _cts = new();
        private int _nextSensorIndex = 6;

        private const int TargetActiveSensors = 5;

        public SensorManager(RSA serverPublicKey, HttpClient httpClient)
        {
            _serverPublicKey = serverPublicKey;
            _http = httpClient;
        }

        public async Task StartAsync()
        {
            Console.WriteLine("=== SNUS Sensor Simulator ===");
            Console.WriteLine($"Starting {TargetActiveSensors} initial sensors...\n");

            var configs = SensorConfig.CreateDefaultSensors();

            foreach (var cfg in configs)
            {
                var sensor = new SensorNode(cfg, _serverPublicKey, _http);
                _allSensors.Add(sensor);
                _sensorTasks.Add(sensor.RunAsync(_cts.Token));
            }

            _ = Task.Run(WatchdogLoopAsync);

            await InteractiveCommandLoopAsync();
        }

        public async Task StopAsync()
        {
            await _cts.CancelAsync();
            try { await Task.WhenAll(_sensorTasks); } catch {  }
            Console.WriteLine("\n=== Simulator stopped ===");
        }


        private async Task WatchdogLoopAsync()
        {
            while (!_cts.Token.IsCancellationRequested)
            {
                await Task.Delay(2000, _cts.Token).ContinueWith(_ => { });

                int activeCount = _allSensors.Count(s => s.IsActive);

                if (activeCount < TargetActiveSensors)
                {
                    int needed = TargetActiveSensors - activeCount;
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine($"\n[WATCHDOG] Active sensors: {activeCount}/{TargetActiveSensors}. Spawning {needed} replacement(s)...");
                    Console.ResetColor();

                    for (int i = 0; i < needed; i++)
                        SpawnReplacementSensor();
                }
            }
        }

        private void SpawnReplacementSensor()
        {
            var rng = new Random();
            string newId = $"SENSOR-{_nextSensorIndex++:D2}";

            var cfg = new SensorConfig(
                id: newId,
                tempMin: 238 + rng.NextDouble() * 5,
                tempMax: 320 + rng.NextDouble() * 10,
                quality: DataQuality.Good,
                thresholds: new AlarmThresholds
                {
                    Priority1Low = 268,
                    Priority1High = 310,
                    Priority2Low = 258,
                    Priority2High = 315,
                    Priority3Low = 248,
                    Priority3High = 320
                });

            var sensor = new SensorNode(cfg, _serverPublicKey, _http);
            _allSensors.Add(sensor);
            _sensorTasks.Add(sensor.RunAsync(_cts.Token));

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"[WATCHDOG] Replacement sensor {newId} is now active.");
            Console.ResetColor();
        }


        private async Task InteractiveCommandLoopAsync()
        {
            PrintHelp();

            while (!_cts.IsCancellationRequested)
            {
                if (!Console.KeyAvailable)
                {
                    await Task.Delay(100);
                    continue;
                }

                Console.Write("\nCommand> ");
                string? input = Console.ReadLine()?.Trim();
                if (string.IsNullOrWhiteSpace(input)) continue;

                var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                string cmd = parts[0].ToUpperInvariant();

                switch (cmd)
                {
                    case "BLOCK":
                        if (parts.Length < 2) { Console.WriteLine("Usage: BLOCK <sensor-id>"); break; }
                        BlockSensor(parts[1]);
                        break;

                    case "STATUS":
                        PrintStatus();
                        break;

                    case "HELP":
                        PrintHelp();
                        break;

                    case "QUIT":
                    case "EXIT":
                        await StopAsync();
                        return;

                    default:
                        Console.WriteLine($"Unknown command: {cmd}. Type HELP.");
                        break;
                }
            }
        }

        private void BlockSensor(string sensorId)
        {
            var sensor = _allSensors.FirstOrDefault(s =>
                s.Id.Equals(sensorId, StringComparison.OrdinalIgnoreCase));

            if (sensor == null)
            {
                Console.WriteLine($"Sensor '{sensorId}' not found.");
                return;
            }

            sensor.BlockTemporarily();
            Console.WriteLine($"Sensor {sensorId} blocked for 30s. Watchdog will spawn replacement.");
        }

        private void PrintStatus()
        {
            Console.WriteLine("\n┌─ Active Sensors ───────────────────────────────┐");
            foreach (var s in _allSensors)
            {
                string status = s.IsActive ? "ACTIVE  " : "BLOCKED ";
                ConsoleColor c = s.IsActive ? ConsoleColor.Green : ConsoleColor.Red;
                Console.ForegroundColor = c;
                Console.WriteLine($"│  {s.Id}  {status}  Q={s.Quality}");
            }
            Console.ResetColor();
            int active = _allSensors.Count(s => s.IsActive);
            Console.WriteLine($"└─ Total: {active}/{TargetActiveSensors} active ──────────────────────────┘\n");
        }

        private static void PrintHelp()
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("\n┌─ Commands ─────────────────────────────────────┐");
            Console.WriteLine("│  STATUS                 Show all sensors       │");
            Console.WriteLine("│  BLOCK <sensor-id>      Block sensor 30s       │");
            Console.WriteLine("│  HELP                   Show this help         │");
            Console.WriteLine("│  QUIT                   Stop simulator         │");
            Console.WriteLine("└────────────────────────────────────────────────┘\n");
            Console.ResetColor();
        }
    }
}
