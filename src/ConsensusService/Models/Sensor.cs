using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsensusService.Models
{
    public class Sensor
    {
        public string Id { get; set; } = null!;
        public double MinRange { get; set; }
        public double MaxRange { get; set; }
        public SensorQuality Quality { get; set; } = SensorQuality.GOOD;

        public double AlarmThreshold1 { get; set; }
        public double AlarmThreshold2 { get; set; }
        public double AlarmThreshold3 { get; set; }
        public DateTime LastMessageAt { get; set; }

    }
}
