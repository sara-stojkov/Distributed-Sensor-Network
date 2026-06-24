using AspNetCoreRateLimit;
using IngestionService.Data;
using IngestionService.RateLimiting;
using IngestionService.Services;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

var builder = WebApplication.CreateBuilder(args);

string keysDir = builder.Configuration["Keys:Directory"] ?? "keys";
string privateKeyPath = Path.Combine(keysDir, "server_private.pem");
string publicKeyPath = Path.Combine(keysDir, "server_public.pem");

Directory.CreateDirectory(keysDir);

RSA serverRsa;
if (!File.Exists(privateKeyPath) || !File.Exists(publicKeyPath))
{
    Console.WriteLine("[Keys] Generating new RSA-2048 key pair...");
    serverRsa = CryptoService.GenerateRsaKeyPair();
    await File.WriteAllTextAsync(privateKeyPath, CryptoService.ExportPrivateKeyPem(serverRsa));
    await File.WriteAllTextAsync(publicKeyPath, CryptoService.ExportPublicKeyPem(serverRsa));
    Console.WriteLine($"[Keys] Saved to {keysDir}/");
}
else
{
    Console.WriteLine("[Keys] Loading existing RSA key pair...");
    serverRsa = CryptoService.LoadPrivateKeyPem(await File.ReadAllTextAsync(privateKeyPath));
}

string serverPublicKeyPem = await File.ReadAllTextAsync(publicKeyPath);

var connectionString = builder.Configuration.GetConnectionString("Default");
builder.Services.AddDbContextFactory<IngestionDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddControllers();
builder.Services.AddSingleton(serverRsa);
builder.Services.AddSingleton(new ServerKeyProvider(serverPublicKeyPem));
builder.Services.AddSingleton<SensorRegistryService>();
builder.Services.AddSingleton<AlarmNotificationService>();
builder.Services.AddSingleton<ReplayProtectionService>();
builder.Services.AddScoped<ReadingPersistenceService>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddMemoryCache();

builder.Services.Configure<ClientRateLimitOptions>(
    builder.Configuration.GetSection("ClientRateLimiting"));
builder.Services.Configure<ClientRateLimitPolicies>(
    builder.Configuration.GetSection("ClientRateLimitPolicies"));

builder.Services.AddInMemoryRateLimiting();

builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

builder.Services.AddSingleton<IClientResolveContributor, SensorIdClientResolveContributor>();

builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.Use(async (context, next) =>
{
    context.Request.EnableBuffering();
    await next();
});

app.UseClientRateLimiting();

app.UseCors();

app.UseAuthorization();

app.MapControllers();

Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine("╔══════════════════════════════════════════════════╗");
Console.WriteLine("║       IngestionService                           ║");
Console.WriteLine("║       Listening for sensor readings...           ║");
Console.WriteLine("╚══════════════════════════════════════════════════╝");
Console.ResetColor();

app.Run();

public class ServerKeyProvider(string publicKeyPem)
{
    public string PublicKeyPem { get; } = publicKeyPem;
}