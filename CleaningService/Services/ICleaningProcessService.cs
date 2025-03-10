using System.Threading.Tasks;
using CleaningService.Models;

namespace CleaningService.Services
{
    public interface ICleaningProcessService
    {
        Task<RequestCleaningResponse> ProcessCleaningRequest(RequestCleaningInput request);
    }
}
