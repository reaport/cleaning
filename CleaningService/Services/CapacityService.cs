using CleaningService.Models;
using Microsoft.Extensions.Logging;

namespace CleaningService.Services
{
    public class CapacityService : ICapacityService
    {
        private int _waterCapacity = 100; // Значение по умолчанию
        private readonly ILogger<CapacityService> _logger;

        public CapacityService(ILogger<CapacityService> logger)
        {
            _logger = logger;
        }

        public int GetCapacity() => _waterCapacity;

        public void UpdateCapacity(int capacity)
        {
            if (capacity < 0) return;
            _waterCapacity = capacity;
            _logger.LogInformation("CapacityService: Updated water capacity to {Capacity}", capacity);
        }
    }
}
