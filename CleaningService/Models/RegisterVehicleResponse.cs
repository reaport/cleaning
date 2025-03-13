using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CleaningService.Models
{
    public class RegisterVehicleResponse
    {
        [JsonPropertyName("GarrageNodeId")]
        public string GarageNodeId { get; set; }
        public string VehicleId { get; set; }
        public Dictionary<string, string> ServiceSpots { get; set; }
    }
}
