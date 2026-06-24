using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsensusService.Models
{
    public class SensorReading
    {
        public long Id { get; set; }
        public string SensorId { get; set; } = null!;
        public Sensor? Sensor { get; set; }
        public double Value { get; set; }
        public DateTime Timestamp { get; set; }
        public SensorQuality Quality { get; set; } = SensorQuality.GOOD;
        public int AlarmPriority { get; set; } // 0-3
        public bool IsConsensus { get; set; } = false;
    }
}
