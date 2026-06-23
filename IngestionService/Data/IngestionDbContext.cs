using Microsoft.EntityFrameworkCore;

namespace IngestionService.Data
{
 
public class IngestionDbContext : DbContext
    {
        public IngestionDbContext(DbContextOptions<IngestionDbContext> options)
            : base(options) { }

        public DbSet<SensorEntity> Sensors => Set<SensorEntity>();
        public DbSet<SensorReadingEntity> SensorReadings => Set<SensorReadingEntity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<SensorEntity>().ToTable("Sensors");
            modelBuilder.Entity<SensorReadingEntity>().ToTable("SensorReadings");

            modelBuilder.Entity<SensorEntity>()
                .Property(s => s.Quality)
                .HasConversion<string>();

            modelBuilder.Entity<SensorReadingEntity>()
                .Property(r => r.Quality)
                .HasConversion<string>();
        }
    }

    public class SensorEntity
    {
        public string Id { get; set; } = null!;
        public double MinRange { get; set; }
        public double MaxRange { get; set; }
        public SensorQualityDb Quality { get; set; } = SensorQualityDb.GOOD;
        public double AlarmThreshold1 { get; set; }
        public double AlarmThreshold2 { get; set; }
        public double AlarmThreshold3 { get; set; }
        public DateTime LastMessageAt { get; set; }
    }

    public class SensorReadingEntity
    {
        public long Id { get; set; }
        public string SensorId { get; set; } = null!;
        public double Value { get; set; }
        public DateTime Timestamp { get; set; }
        public SensorQualityDb Quality { get; set; } = SensorQualityDb.GOOD;
        public int AlarmPriority { get; set; }
        public bool IsConsensus { get; set; } = false;  // always false from our side
    }

    public enum SensorQualityDb
    {
        GOOD,
        BAD,
        UNCERTAIN
    }
}
