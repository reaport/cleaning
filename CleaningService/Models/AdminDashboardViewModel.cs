using System.Collections.Generic;

namespace CleaningService.Models
{
    /// <summary>
    /// Модель для отображения информации на странице админки.
    /// </summary>
    public class AdminDashboardViewModel
    {
        // Например, водная вместимость транспортных средств (для заправки)
        public int WaterCapacity { get; set; }
        // Режим работы модуля (например, "Mock" или "Real")
        public string ModuleMode { get; set; }
        // Список транспортных средств для клининга
        public List<CleaningVehicleStatusInfo> Vehicles { get; set; }
    }
}
