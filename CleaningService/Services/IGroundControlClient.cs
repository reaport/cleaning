using System.Threading.Tasks;
using CleaningService.Models;

namespace CleaningService.Services
{
    public interface IGroundControlClient
    {
        Task<CleaningVehicleStatusInfo> RegisterVehicleAsync(string vehicleType);
        Task<string[]?> FetchRouteAsync(string from, string to, string type);
        Task<double?> RequestPermissionAsync(string vehicleId, string from, string to, string vehicleType);
        Task NotifyArrivalAsync(string vehicleId, string nodeId, string vehicleType);
    }
}
