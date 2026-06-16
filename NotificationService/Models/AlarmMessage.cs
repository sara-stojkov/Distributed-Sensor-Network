namespace NotificationService.Models
{
    public class AlarmMessage
    {
        public string SensorId { get; set; } = string.Empty;
        public double Value { get; set; }
        public int Priority { get; set; }   // 1, 2 ili 3
        public DateTime Timestamp { get; set; }
    }
}