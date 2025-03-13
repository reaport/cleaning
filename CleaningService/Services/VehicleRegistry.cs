using System.Collections.Generic;
using System.Linq;
using CleaningService.Models;
using Microsoft.Extensions.Logging;

namespace CleaningService.Services
{
    public class VehicleRegistry : IVehicleRegistry
    {
        // Внутренний класс для хранения информации о транспортном средстве
        private class VehicleInfo
        {
            public string VehicleId { get; set; }
            public string BaseNode { get; set; }
            public string CurrentNode { get; set; }
            public string Status { get; set; } // "Available" или "Busy"
            public Dictionary<string, string> ServiceSpots { get; set; }
        }

        // Храним информацию по ТС: ключ – VehicleId, значение – объект VehicleInfo
        private readonly Dictionary<string, VehicleInfo> _vehicles = new Dictionary<string, VehicleInfo>();
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
                foreach (var vehicle in _vehicles.Values)
                {
                    if (vehicle.Status == "Available")
                    {
                        vehicle.Status = "Busy";
                        _logger.LogInformation("AcquireAvailableVehicle: using {VehicleId} for aircraft {AircraftId}", vehicle.VehicleId, aircraftId);
                        string destination = null;
                        if (vehicle.ServiceSpots != null && vehicle.ServiceSpots.ContainsKey(aircraftId))
                        {
                            destination = vehicle.ServiceSpots[aircraftId];
                        }
                        return (vehicle.VehicleId, vehicle.BaseNode, destination);
                    }
                }
            }
            _logger.LogWarning("AcquireAvailableVehicle: no free vehicle for aircraft {AircraftId}", aircraftId);
            return (null, null, null);
        }

        public void ReleaseVehicle(string vehicleId, string baseNode)
        {
            lock (_syncLock)
            {
                if (_vehicles.ContainsKey(vehicleId))
                {
                    _vehicles[vehicleId].Status = "Available";
                    _vehicles[vehicleId].CurrentNode = baseNode;
                    _logger.LogInformation("ReleaseVehicle: vehicle {VehicleId} returned to base {BaseNode}", vehicleId, baseNode);
                }
            }
        }

        public void AddVehicle(string vehicleId, string baseNode, Dictionary<string, string> serviceSpots)
        {
            lock (_syncLock)
            {
                if (_vehicles.Count < 5)
                {
                    _vehicles[vehicleId] = new VehicleInfo
                    {
                        VehicleId = vehicleId,
                        BaseNode = baseNode,
                        CurrentNode = baseNode,
                        Status = "Available",
                        ServiceSpots = serviceSpots
                    };
                    _logger.LogInformation("AddVehicle: {VehicleId} added at base {BaseNode}", vehicleId, baseNode);
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
                return _vehicles.Count < 5;
            }
        }

        public bool TryAddVehicle(string vehicleId, string baseNode, Dictionary<string, string> serviceSpots)
        {
            lock (_syncLock)
            {
                if (_vehicles.Count < 5)
                {
                    _vehicles[vehicleId] = new VehicleInfo
                    {
                        VehicleId = vehicleId,
                        BaseNode = baseNode,
                        CurrentNode = baseNode,
                        Status = "Available",
                        ServiceSpots = serviceSpots
                    };
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
                if (_vehicles.ContainsKey(vehicleId))
                {
                    _vehicles[vehicleId].Status = "Busy";
                    _logger.LogInformation("MarkAsBusy: vehicle {VehicleId} marked as Busy", vehicleId);
                }
            }
        }

        public void MarkAsAvailable(string vehicleId, string baseNode)
        {
            lock (_syncLock)
            {
                if (_vehicles.ContainsKey(vehicleId))
                {
                    _vehicles[vehicleId].Status = "Available";
                    _vehicles[vehicleId].CurrentNode = baseNode;
                    _logger.LogInformation("MarkAsAvailable: vehicle {VehicleId} marked as Available", vehicleId);
                }
            }
        }

        public void UpdateCurrentNode(string vehicleId, string currentNode)
        {
            lock (_syncLock)
            {
                if (_vehicles.ContainsKey(vehicleId))
                {
                    _vehicles[vehicleId].CurrentNode = currentNode;
                    _logger.LogInformation("UpdateCurrentNode: vehicle {VehicleId} current node updated to {CurrentNode}", vehicleId, currentNode);
                }
            }
        }

        public IEnumerable<CleaningVehicleStatusInfo> GetAllVehicles()
        {
            lock (_syncLock)
            {
                return _vehicles.Values.Select(v => new CleaningVehicleStatusInfo
                {
                    VehicleId = v.VehicleId,
                    BaseNode = v.BaseNode,
                    Status = v.Status,
                    CurrentNode = v.CurrentNode
                }).ToList();
            }
        }

        public void Reset()
        {
            lock (_syncLock)
            {
                _vehicles.Clear();
                _logger.LogInformation("Vehicle registry reset.");
            }
        }
    }
}
