using CleaningService.Models;

namespace CleaningService.Services
{
    public interface ICapacityService
    {
        int GetCapacity();
        void UpdateCapacity(int capacity);
    }
}
