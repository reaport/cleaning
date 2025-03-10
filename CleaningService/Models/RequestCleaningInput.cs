namespace CleaningService.Models
{
    /// <summary>
    /// Модель входного запроса на очистку самолёта.
    /// </summary>
    public class RequestCleaningInput
    {
        public string AircraftId { get; set; }
        public string NodeId { get; set; }
        public int WaterAmount { get; set; }
    }
}
