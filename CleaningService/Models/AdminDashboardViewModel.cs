using System.Collections.Generic;

namespace CleaningService.Models
{
    public class AdminViewModel
    {
        public AdminConfig Config { get; set; }
        public IEnumerable<CleaningVehicleStatusInfo> Vehicles { get; set; }
        public string Mode { get; set; }  // "Mock" или "Real"
    }
}
