using NotificationService.Hubs;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSignalR();

// CORS je potreban da bi klijenti (npr. test stranica ili druga app)
// mogli da se konektuju na hub sa drugog porta/adrese.
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyHeader()
              .AllowAnyMethod()
              .SetIsOriginAllowed(_ => true)
              .AllowCredentials(); // SignalR zahteva ovo umesto AllowAnyOrigin
    });
});

var app = builder.Build();

app.UseCors("AllowAll");

app.MapControllers();
app.MapHub<AlarmHub>("/hubs/alarms");

app.MapGet("/health", () => Results.Ok(new { status = "NotificationService is running" }));

app.Run();