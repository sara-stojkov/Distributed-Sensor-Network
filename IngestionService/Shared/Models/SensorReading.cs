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
    }

    public class SecureMessage
    {
        // AES-encrypted + Base64 payload
        public string EncryptedPayload { get; set; } = string.Empty;
        // AES key encrypted with server's RSA public key
        public string EncryptedAesKey { get; set; } = string.Empty;
        // AES IV
        public string IV { get; set; } = string.Empty;
        // ECDSA/RSA signature over EncryptedPayload
        public string Signature { get; set; } = string.Empty;
        // Sensor's public key (DER, Base64) for identity verification
        public string SenderPublicKey { get; set; } = string.Empty;
        // ISO8601 timestamp of sending, replay protection
        public string SentAt { get; set; } = string.Empty;
        // Monotonic message counter per sensor
        public long MessageId { get; set; }
        // Sensor ID in plaintext for routing/rate-limiting before decrypt
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
