namespace CleaningService.Models
{
    public class UpdateConfigRequest
    {
        public int ConflictRetryCount { get; set; }
        public double MovementSpeed { get; set; }
        public int NumberOfCleaningVehicles { get; set; }
    }
}
