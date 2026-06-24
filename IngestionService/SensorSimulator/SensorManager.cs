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
        private readonly object _slotLock = new();
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
                sensor.AssignSlot();
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
                await Task.Delay(1000, _cts.Token).ContinueWith(_ => { });

                lock (_slotLock)
                {
                    int slotsHeld = _allSensors.Count(s => s.HasActiveSlot);
                    int openSlots = TargetActiveSensors - slotsHeld;

                    if (openSlots <= 0) continue;

                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine($"\n[WATCHDOG] {slotsHeld}/{TargetActiveSensors} slots filled. Filling {openSlots} open slot(s)...");
                    Console.ResetColor();

                    for (int i = 0; i < openSlots; i++)
                        FillOneOpenSlot();
                }
            }
        }

        private void FillOneOpenSlot()
        {
            var reserveSensor = _allSensors.FirstOrDefault(s => !s.HasActiveSlot && s.IsBlockExpired);

            if (reserveSensor != null)
            {
                reserveSensor.AssignSlot();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"[WATCHDOG] {reserveSensor.Id} re-admitted from reserve into the active pool.");
                Console.ResetColor();
                return;
            }

            SpawnReplacementSensor();
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
            sensor.AssignSlot();
            _allSensors.Add(sensor);
            _sensorTasks.Add(sensor.RunAsync(_cts.Token));

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"[WATCHDOG] New replacement sensor {newId} spawned and assigned a slot.");
            Console.ResetColor();
        }

        private static bool HasInteractiveConsole()
        {
            try
            {
                _ = Console.KeyAvailable;
                return true;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        }

        private async Task InteractiveCommandLoopAsync()
        {
            if (!HasInteractiveConsole())
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("[INFO] No interactive console detected (running in Kubernetes / redirected stdin).");
                Console.WriteLine("[INFO] Simulator is running headlessly. Send SIGTERM to stop.");
                Console.ResetColor();

                await Task.Delay(Timeout.Infinite, _cts.Token).ContinueWith(_ => { });
                return;
            }

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

            lock (_slotLock)
            {
                bool hadSlot = sensor.HasActiveSlot;
                sensor.BlockTemporarily();

                if (hadSlot)
                {
                    sensor.ReleaseSlot();
                    Console.WriteLine($"Sensor {sensorId} blocked for 30s. Slot released — watchdog will fill it on the next tick.");
                }
                else
                {
                    Console.WriteLine($"Sensor {sensorId} blocked for 30s (was already in reserve, no slot to release).");
                }
            }
        }

        private void PrintStatus()
        {
            Console.WriteLine("\n┌─ Sensor Pool ───────────────────────────────────┐");
            foreach (var s in _allSensors)
            {
                string status = s.HasActiveSlot ? "ACTIVE (slot)" : (s.IsBlockExpired ? "RESERVE      " : "BLOCKED      ");
                ConsoleColor c = s.HasActiveSlot ? ConsoleColor.Green
                               : s.IsBlockExpired ? ConsoleColor.DarkCyan
                               : ConsoleColor.Red;
                Console.ForegroundColor = c;
                Console.WriteLine($"│  {s.Id}  {status}  Q={s.Quality}");
            }
            Console.ResetColor();
            int active = _allSensors.Count(s => s.HasActiveSlot);
            Console.WriteLine($"└─ Total: {active}/{TargetActiveSensors} slots filled ──────────────────────────┘\n");
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
