using IngestionService.Models;

namespace SensorSimulator
{
    public class SensorConfig
    {
        public string Id { get; init; }
        public double TempMin { get; init; }
        public double TempMax { get; init; }
        public DataQuality Quality { get; init; }
        public AlarmThresholds Thresholds { get; init; }

        public SensorConfig(
            string id,
            double tempMin,
            double tempMax,
            DataQuality quality,
            AlarmThresholds thresholds)
        {
            Id = id;
            TempMin = tempMin;
            TempMax = tempMax;
            Quality = quality;
            Thresholds = thresholds;
        }

        public static List<SensorConfig> CreateDefaultSensors() =>
        [
            new("SENSOR-01", 240.0, 330.0, DataQuality.Good, new AlarmThresholds
            {
                Priority1Low = 270.0, Priority1High = 310.0,
                Priority2Low = 260.0, Priority2High = 315.0,
                Priority3Low = 250.0, Priority3High = 320.0
            }),
            new("SENSOR-02", 235.0, 335.0, DataQuality.Good, new AlarmThresholds
            {
                Priority1Low = 265.0, Priority1High = 312.0,
                Priority2Low = 255.0, Priority2High = 318.0,
                Priority3Low = 245.0, Priority3High = 325.0
            }),
            new("SENSOR-03", 245.0, 325.0, DataQuality.Uncertain, new AlarmThresholds
            {
                Priority1Low = 272.0, Priority1High = 308.0,
                Priority2Low = 262.0, Priority2High = 313.0,
                Priority3Low = 255.0, Priority3High = 315.0
            }),
            new("SENSOR-04", 238.0, 332.0, DataQuality.Good, new AlarmThresholds
            {
                Priority1Low = 268.0, Priority1High = 310.0,
                Priority2Low = 258.0, Priority2High = 316.0,
                Priority3Low = 248.0, Priority3High = 322.0
            }),
            new("SENSOR-05", 242.0, 328.0, DataQuality.Good, new AlarmThresholds
            {
                Priority1Low = 270.0, Priority1High = 309.0,
                Priority2Low = 260.0, Priority2High = 314.0,
                Priority3Low = 252.0, Priority3High = 318.0
            }),
        ];
    }
}
