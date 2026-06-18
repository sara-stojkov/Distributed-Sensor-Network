using IngestionService.Services;
using System.Security.Cryptography;

namespace SensorSimulator
{
    internal static class Program
    {
        private static async Task Main()
        {
            string serverUrl = Environment.GetEnvironmentVariable("INGESTION_URL")
                ?? "http://localhost:5050";

            string serverPubKeyPath = Environment.GetEnvironmentVariable("SERVER_PUBKEY_PATH")
                ?? "keys/server_public.pem";

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("╔══════════════════════════════════════════════════╗");
            Console.WriteLine("║       Distributed Sensor System                  ║");
            Console.WriteLine("║       Sensor Simulator                           ║");
            Console.WriteLine($"║       Server: {serverUrl,-35}║");
            Console.WriteLine("╚══════════════════════════════════════════════════╝");
            Console.ResetColor();

            RSA serverPublicKey;

            if (File.Exists(serverPubKeyPath))
            {
                Console.WriteLine($"Loading server public key from {serverPubKeyPath}...");
                string pem = await File.ReadAllTextAsync(serverPubKeyPath);
                serverPublicKey = CryptoService.LoadPublicKeyPem(pem);
            }
            else
            {
                Console.WriteLine($"Key file not found at '{serverPubKeyPath}'.");
                Console.WriteLine($"Attempting to fetch server public key from {serverUrl}/api/keys/server-public-key ...");

                using var tempClient = new HttpClient { BaseAddress = new Uri(serverUrl) };
                try
                {
                    int retries = 10;
                    while (retries-- > 0)
                    {
                        try
                        {
                            var keyResponse = await tempClient.GetAsync("/api/keys/server-public-key");
                            keyResponse.EnsureSuccessStatusCode();
                            string pem = await keyResponse.Content.ReadAsStringAsync();

                            Directory.CreateDirectory(Path.GetDirectoryName(serverPubKeyPath)!);
                            await File.WriteAllTextAsync(serverPubKeyPath, pem);
                            Console.WriteLine("Server public key fetched and cached.");

                            serverPublicKey = CryptoService.LoadPublicKeyPem(pem);
                            break;
                        }
                        catch
                        {
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine($"Server not ready, retrying in 3s... ({retries} attempts left)");
                            Console.ResetColor();
                            await Task.Delay(3000);
                        }
                    }

                    if (retries < 0)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Could not reach the server. Exiting.");
                        Console.ResetColor();
                        return;
                    }
                    serverPublicKey = CryptoService.LoadPublicKeyPem(
                        await File.ReadAllTextAsync(serverPubKeyPath));
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Fatal: {ex.Message}");
                    Console.ResetColor();
                    return;
                }
            }

            var http = new HttpClient
            {
                BaseAddress = new Uri(serverUrl),
                Timeout = TimeSpan.FromSeconds(10)
            };

            var manager = new SensorManager(serverPublicKey, http);

            Console.CancelKeyPress += async (_, e) =>
            {
                e.Cancel = true;
                Console.WriteLine("\nShutting down...");
                await manager.StopAsync();
                Environment.Exit(0);
            };

            await manager.StartAsync();
        }
    }
}