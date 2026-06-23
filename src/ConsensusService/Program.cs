using ConsensusService;
using ConsensusService.Data;
using ConsensusService.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddSingleton<BftConsensusCalculator>();
builder.Services.AddHostedService<Worker>();
builder.Services.AddControllers();

var app = builder.Build();

app.MapControllers();

app.Run();