using Newtonsoft.Json;

namespace Esp32_room_sensor_data.Models
{
    public class SensorData
    {
        // Ensure ID is unique if not provided
        [JsonProperty("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [JsonProperty("temperature")]
        public float Temperature { get; set; }

        [JsonProperty("humidity")]
        public float Humidity { get; set; }

        [JsonProperty("gasVoltage")]
        public float GasVoltage { get; set; }

        // Partition key - Timestamp must be in ISO 8601 format (UTC)
        [JsonProperty("timestamp")]
        public string Timestamp { get; set; } = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
    }
}
