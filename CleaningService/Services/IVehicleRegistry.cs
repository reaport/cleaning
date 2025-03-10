using System.Collections.Generic;
using CleaningService.Models;

namespace CleaningService.Services
{
    public interface IVehicleRegistry
    {
        (string? VehicleId, string? BaseNode, string? Destination) AcquireAvailableVehicle(string aircraftId);
        void ReleaseVehicle(string vehicleId, string baseNode);
        void AddVehicle(string vehicleId, string baseNode, Dictionary<string, string> serviceSpots);
        bool CanRegisterNewVehicle();
        bool TryAddVehicle(string vehicleId, string baseNode, Dictionary<string, string> serviceSpots);

        void MarkAsBusy(string vehicleId);
        void MarkAsAvailable(string vehicleId, string baseNode);

        IEnumerable<CleaningVehicleStatusInfo> GetAllVehicles();
    }
}
