namespace IngestionService.Models
{
    public class SensorReading
    {
        public string SensorId { get; set; } = string.Empty;
        public double Temperature { get; set; }
        public DateTime Timestamp { get; set; }
        public DataQuality Quality { get; set; }
        public int AlarmPriority { get; set; }
        public long MessageId { get; set; }

        public double MinRange { get; set; }
        public double MaxRange { get; set; }

        public double AlarmThreshold1 { get; set; }
        public double AlarmThreshold2 { get; set; }
        public double AlarmThreshold3 { get; set; }
    }

    public class SecureMessage
    {
        public string EncryptedPayload { get; set; } = string.Empty;
        public string EncryptedAesKey { get; set; } = string.Empty;
        public string IV { get; set; } = string.Empty;
        public string Signature { get; set; } = string.Empty;
        public string SenderPublicKey { get; set; } = string.Empty;
        public string SentAt { get; set; } = string.Empty;
        public long MessageId { get; set; }
        public string SensorId { get; set; } = string.Empty;
    }

    public class AlarmThresholds
    {
        public double Priority1Low { get; set; }
        public double Priority1High { get; set; }
        public double Priority2Low { get; set; }
        public double Priority2High { get; set; }
        public double Priority3Low { get; set; }
        public double Priority3High { get; set; }
    }

    public enum DataQuality
    {
        Good = 0,
        Uncertain = 1,
        Bad = 2
    }
}
