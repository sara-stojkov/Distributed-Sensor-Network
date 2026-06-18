using IngestionService.Models;
using IngestionService.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace SensorSimulator
{
    public class SensorNode
    {
        private readonly SensorConfig _config;
        private readonly CryptoService _crypto;
        private readonly HttpClient _http;
        private readonly Random _rng = new();
        private long _messageCounter = 0;
        private bool _isBlocked = false;
        private DateTime _blockedUntil = DateTime.MinValue;

        private readonly int _intervalMinMs;
        private readonly int _intervalMaxMs;

        public string Id => _config.Id;
        public DataQuality Quality => _config.Quality;
        public bool IsActive => !_isBlocked || DateTime.Now >= _blockedUntil;

        public SensorNode(
            SensorConfig config,
            RSA serverPublicKey,
            HttpClient httpClient,
            int intervalMinMs = 1000,
            int intervalMaxMs = 10000)
        {
            _config = config;
            _http = httpClient;
            _intervalMinMs = intervalMinMs;
            _intervalMaxMs = intervalMaxMs;

            var sensorKeyPair = CryptoService.GenerateRsaKeyPair();
            _crypto = new CryptoService(sensorKeyPair, serverPublicKey);

            ConsoleLog($"Initialized. Public key: {_crypto.SensorPublicKeyBase64[..32]}...", ConsoleColor.DarkGray);
        }

        public async Task RunAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                if (_isBlocked && DateTime.Now < _blockedUntil)
                {
                    await Task.Delay(500, ct);
                    continue;
                }
                _isBlocked = false;

                int intervalMs = _rng.Next(_intervalMinMs, _intervalMaxMs);
                await Task.Delay(intervalMs, ct);

                var reading = GenerateReading();
                int alarmPriority = EvaluateAlarm(reading.Temperature);
                reading.AlarmPriority = alarmPriority;

                PrintReading(reading, alarmPriority);

                await SendToServerAsync(reading, ct);
            }
        }

        public void BlockTemporarily()
        {
            _isBlocked = true;
            _blockedUntil = DateTime.Now.AddSeconds(30);
            ConsoleLog("BLOCKED for 30 seconds (fault-tolerance test)", ConsoleColor.Magenta);
        }


        private SensorReading GenerateReading()
        {
            double temp = _config.TempMin + _rng.NextDouble() * (_config.TempMax - _config.TempMin);

            return new SensorReading
            {
                SensorId = _config.Id,
                Temperature = Math.Round(temp, 2),
                Timestamp = DateTime.Now,
                Quality = _config.Quality,
                MessageId = Interlocked.Increment(ref _messageCounter)
            };
        }

        private int EvaluateAlarm(double temp)
        {
            var t = _config.Thresholds;

            if (temp <= t.Priority3Low || temp >= t.Priority3High)
                return 3;

            if (temp <= t.Priority2Low || temp >= t.Priority2High)
                return 2;

            if (temp <= t.Priority1Low || temp >= t.Priority1High)
                return 1;

            return 0;
        }

        private void PrintReading(SensorReading reading, int alarmPriority)
        {
            var color = alarmPriority switch
            {
                1 => ConsoleColor.Yellow,
                2 => ConsoleColor.DarkYellow, 
                3 => ConsoleColor.Red,
                _ => ConsoleColor.White
            };

            string alarmLabel = alarmPriority == 0
                ? ""
                : $"  ALARM P{alarmPriority}";

            ConsoleLog(
                $"T={reading.Temperature:F2}°C  Q={reading.Quality}  MSG#{reading.MessageId}{alarmLabel}",
                color);
        }

        private async Task SendToServerAsync(SensorReading reading, CancellationToken ct)
        {
            try
            {
                var secureMsg = _crypto.Encrypt(reading);
                var json = JsonSerializer.Serialize(secureMsg);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                var response = await _http.PostAsync("/api/ingest/reading", content, ct);

                if (!response.IsSuccessStatusCode)
                {
                    ConsoleLog($"Server returned {(int)response.StatusCode}", ConsoleColor.DarkRed);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                ConsoleLog($"Send failed: {ex.Message}", ConsoleColor.DarkRed);
            }
        }

        private void ConsoleLog(string msg, ConsoleColor color = ConsoleColor.White)
        {
            lock (Console.Out)
            {
                var prev = Console.ForegroundColor;
                Console.ForegroundColor = color;
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] [{_config.Id}] {msg}");
                Console.ForegroundColor = prev;
            }
        }
    }
}
