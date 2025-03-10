using System.Collections.Generic;
using System.Linq;
using CleaningService.Models;
using Microsoft.Extensions.Logging;

namespace CleaningService.Services
{
    public class VehicleRegistry : IVehicleRegistry
    {
        private readonly Dictionary<string, string> _vehicleStatus = new Dictionary<string, string>();
        private readonly Dictionary<string, Dictionary<string, string>> _serviceMapping = new Dictionary<string, Dictionary<string, string>>();
        private readonly object _syncLock = new object();
        private readonly ILogger<VehicleRegistry> _logger;

        public VehicleRegistry(ILogger<VehicleRegistry> logger)
        {
            _logger = logger;
        }

        public (string? VehicleId, string? BaseNode, string? Destination) AcquireAvailableVehicle(string aircraftId)
        {
            lock (_syncLock)
            {
                var entry = _vehicleStatus.FirstOrDefault(kvp => kvp.Value != "in use");
                if (!string.IsNullOrEmpty(entry.Key))
                {
                    string vehicleId = entry.Key;
                    string baseNode = entry.Value;
                    string? destination = null;
                    if (_serviceMapping.ContainsKey(vehicleId) && _serviceMapping[vehicleId].ContainsKey(aircraftId))
                    {
                        destination = _serviceMapping[vehicleId][aircraftId];
                    }
                    _vehicleStatus[vehicleId] = "in use";
                    _logger.LogInformation("AcquireAvailableVehicle: using {VehicleId} for aircraft {AircraftId}", vehicleId, aircraftId);
                    return (vehicleId, baseNode, destination);
                }
            }
            _logger.LogWarning("AcquireAvailableVehicle: no free vehicle for aircraft {AircraftId}", aircraftId);
            return (null, null, null);
        }

        public void ReleaseVehicle(string vehicleId, string baseNode)
        {
            lock (_syncLock)
            {
                if (_vehicleStatus.ContainsKey(vehicleId))
                {
                    _vehicleStatus[vehicleId] = baseNode;
                    _logger.LogInformation("ReleaseVehicle: vehicle {VehicleId} returned to base {BaseNode}", vehicleId, baseNode);
                }
            }
        }

        public void AddVehicle(string vehicleId, string baseNode, Dictionary<string, string> serviceSpots)
        {
            lock (_syncLock)
            {
                if (_vehicleStatus.Count < 5)
                {
                    _vehicleStatus[vehicleId] = baseNode;
                    _serviceMapping[vehicleId] = serviceSpots;
                    _logger.LogInformation("AddVehicle: {VehicleId} at base {BaseNode}", vehicleId, baseNode);
                }
                else
                {
                    _logger.LogWarning("Global vehicle limit reached. Cannot add vehicle {VehicleId}", vehicleId);
                }
            }
        }

        public bool CanRegisterNewVehicle()
        {
            lock (_syncLock)
            {
                return _vehicleStatus.Count < 5;
            }
        }

        public bool TryAddVehicle(string vehicleId, string baseNode, Dictionary<string, string> serviceSpots)
        {
            lock (_syncLock)
            {
                if (_vehicleStatus.Count < 5)
                {
                    _vehicleStatus[vehicleId] = baseNode;
                    _serviceMapping[vehicleId] = serviceSpots;
                    _logger.LogInformation("TryAddVehicle: Vehicle {VehicleId} added.", vehicleId);
                    return true;
                }
                else
                {
                    _logger.LogWarning("TryAddVehicle: Global vehicle limit reached. Cannot add vehicle {VehicleId}.", vehicleId);
                    return false;
                }
            }
        }

        public void MarkAsBusy(string vehicleId)
        {
            lock (_syncLock)
            {
                if (_vehicleStatus.ContainsKey(vehicleId))
                {
                    _vehicleStatus[vehicleId] = "in use";
                    _logger.LogInformation("MarkAsBusy: vehicle {VehicleId} marked as Busy", vehicleId);
                }
            }
        }

        public void MarkAsAvailable(string vehicleId, string baseNode)
        {
            lock (_syncLock)
            {
                if (_vehicleStatus.ContainsKey(vehicleId))
                {
                    _vehicleStatus[vehicleId] = baseNode;
                    _logger.LogInformation("MarkAsAvailable: vehicle {VehicleId} marked as Available", vehicleId);
                }
            }
        }

        public IEnumerable<CleaningVehicleStatusInfo> GetAllVehicles()
        {
            lock (_syncLock)
            {
                var list = new List<CleaningVehicleStatusInfo>();
                foreach (var kvp in _vehicleStatus)
                {
                    string vehicleId = kvp.Key;
                    bool isBusy = kvp.Value == "in use";
                    string baseNode = isBusy ? "N/A" : kvp.Value;
                    Dictionary<string, string> serviceSpots = _serviceMapping.ContainsKey(vehicleId)
                        ? _serviceMapping[vehicleId]
                        : new Dictionary<string, string>();
                    list.Add(new CleaningVehicleStatusInfo
                    {
                        VehicleId = vehicleId,
                        BaseNode = baseNode,
                        Status = isBusy ? "Busy" : "Available",
                        ServiceSpots = serviceSpots
                    });
                }
                return list;
            }
        }
    }
}
