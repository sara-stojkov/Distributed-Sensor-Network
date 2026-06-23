namespace NotificationService.Models
{
    public class AlarmMessage
    {
        public string SensorId { get; set; } = string.Empty;
        public double Temperature { get; set; }   
        public int AlarmPriority { get; set; }     
        public DateTime Timestamp { get; set; }
        public string Quality { get; set; } = string.Empty;   
    }
}