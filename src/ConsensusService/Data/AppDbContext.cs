using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ConsensusService.Models;
using Microsoft.EntityFrameworkCore;

namespace ConsensusService.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Sensor> Sensors => Set<Sensor>();
        public DbSet<SensorReading> SensorReadings => Set<SensorReading>();
        public DbSet<ConsensusValue> ConsensusValues => Set<ConsensusValue>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Sensor>()
                .Property(s => s.Quality)
                .HasConversion<string>();

            modelBuilder.Entity<SensorReading>()
                .Property(r => r.Quality)
                .HasConversion<string>();
        }
    }
}
