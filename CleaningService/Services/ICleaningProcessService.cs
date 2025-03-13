using System.Collections.Generic;
using System.Threading.Tasks;
using CleaningService.Models;

namespace CleaningService.Services
{
    public interface ICleaningProcessService
    {
        Task<RequestCleaningResponse> ProcessCleaningRequest(RequestCleaningInput request);
        Task<bool> RegisterVehicleAsync(string type);
        Task ReloadAsync();
        IEnumerable<CleaningVehicleStatusInfo> GetVehiclesInfo();
    }
}
