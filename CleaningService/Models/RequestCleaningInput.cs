using System.ComponentModel.DataAnnotations;

namespace CleaningService.Models
{
    public class RequestCleaningInput
    {
        [Required]
        public string AircraftId { get; set; }
        [Required]
        public string NodeId { get; set; }
        [Required]
        public int WaterAmount { get; set; }
    }
}
