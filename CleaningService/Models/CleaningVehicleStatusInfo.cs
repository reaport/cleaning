using System.Collections.Generic;

namespace CleaningService.Models
{
    /// <summary>
    /// Модель для отображения статуса транспортного средства клининга.
    /// </summary>
    public class CleaningVehicleStatusInfo
    {
        public string VehicleId { get; set; }
        public string BaseNode { get; set; }
        public string Status { get; set; } // "Busy" или "Available"
        public Dictionary<string, string> ServiceSpots { get; set; }
    }
}
