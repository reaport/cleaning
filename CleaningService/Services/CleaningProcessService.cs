using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CleaningService.Models;
using CleaningService.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace CleaningService.Services
{
    public class CleaningProcessService : ICleaningProcessService
    {
        private readonly IGroundControlClient _groundClient;
        private readonly IVehicleRegistry _vehicleRegistry;
        private readonly ICapacityService _capacityService;
        private readonly IHubContext<VehicleStatusHub> _hubContext;
        private readonly ILogger<CleaningProcessService> _logger;
        private const double VehicleSpeed = 10;

        // Для контроля машин для каждого самолёта (максимум 2)
        private static readonly Dictionary<string, int> FlightVehicleCount = new Dictionary<string, int>();
        private static readonly object FlightLock = new object();

        public CleaningProcessService(
            IGroundControlClient groundClient,
            IVehicleRegistry vehicleRegistry,
            ICapacityService capacityService,
            IHubContext<VehicleStatusHub> hubContext,
            ILogger<CleaningProcessService> logger)
        {
            _groundClient = groundClient;
            _vehicleRegistry = vehicleRegistry;
            _capacityService = capacityService;
            _hubContext = hubContext;
            _logger = logger;
        }

        private async Task WaitUntilFlightVehicleCountLessThan(string aircraftId, int limit)
        {
            while (true)
            {
                int count;
                lock (FlightLock)
                {
                    FlightVehicleCount.TryGetValue(aircraftId, out count);
                }
                if (count < limit)
                    break;
                _logger.LogInformation("Waiting: already {Count} vehicles working for flight {AircraftId}", count, aircraftId);
                await Task.Delay(1000);
            }
        }

        private void IncrementFlightVehicleCount(string aircraftId)
        {
            lock (FlightLock)
            {
                if (FlightVehicleCount.ContainsKey(aircraftId))
                    FlightVehicleCount[aircraftId]++;
                else
                    FlightVehicleCount[aircraftId] = 1;
            }
        }

        private void DecrementFlightVehicleCount(string aircraftId)
        {
            lock (FlightLock)
            {
                if (FlightVehicleCount.ContainsKey(aircraftId))
                {
                    FlightVehicleCount[aircraftId]--;
                    if (FlightVehicleCount[aircraftId] < 0)
                        FlightVehicleCount[aircraftId] = 0;
                }
            }
        }

        public async Task<RequestCleaningResponse> ProcessCleaningRequest(RequestCleaningInput request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.AircraftId))
                    throw new ArgumentException("AircraftId is required");
                if (string.IsNullOrEmpty(request.NodeId))
                    throw new ArgumentException("NodeId is required");
                if (request.WaterAmount < 0)
                    throw new ArgumentException("WaterAmount must be a non-negative integer");

                _logger.LogInformation("Processing cleaning request for AircraftId: {AircraftId} with WaterAmount: {WaterAmount}",
                    request.AircraftId, request.WaterAmount);

                int vehicleCapacity = _capacityService.GetCapacity(); // макс. воды на машину
                int remainingWater = request.WaterAmount;

                // Обрабатываем запрос партиями
                while (remainingWater > 0)
                {
                    await WaitUntilFlightVehicleCountLessThan(request.AircraftId, 2);
                    int vehiclesToDispatch = remainingWater > vehicleCapacity ? 2 : 1;

                    // Глобальный лимит: не более 5 машин
                    while (_vehicleRegistry.GetAllVehicles().Count() >= 5 &&
                           _vehicleRegistry.GetAllVehicles().All(v => v.Status == "Busy"))
                    {
                        _logger.LogInformation("Global vehicle limit reached. Waiting for an available vehicle...");
                        await Task.Delay(1000);
                    }

                    List<Task> batchTasks = new List<Task>();
                    for (int i = 0; i < vehiclesToDispatch; i++)
                    {
                        int waterForVehicle = Math.Min(vehicleCapacity, remainingWater - i * vehicleCapacity);
                        await WaitUntilFlightVehicleCountLessThan(request.AircraftId, 2);
                        IncrementFlightVehicleCount(request.AircraftId);
                        batchTasks.Add(ProcessSingleCleaningOperation(request, waterForVehicle, request.AircraftId));
                    }
                    await Task.WhenAll(batchTasks);
                    remainingWater -= vehiclesToDispatch * vehicleCapacity;
                    if (remainingWater < 0)
                        remainingWater = 0;
                }

                return new RequestCleaningResponse { wait = true };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing cleaning request for AircraftId: {AircraftId}", request.AircraftId);
                throw;
            }
        }

        private async Task ProcessSingleCleaningOperation(RequestCleaningInput request, int waterForVehicle, string flightId)
        {
            string? vehicleId = null;
            string? baseNode = null;
            string destination = request.NodeId;

            // Пытаемся получить свободное транспортное средство
            var vehicleInfo = _vehicleRegistry.AcquireAvailableVehicle(request.AircraftId);
            if (vehicleInfo.VehicleId == null)
            {
                // Если глобальный лимит ещё не достигнут, регистрируем новое; иначе ждём освобождения
                if (_vehicleRegistry.GetAllVehicles().Count() < 5)
                {
                    var regVehicle = await _groundClient.RegisterVehicleAsync("cleaning");
                    if (regVehicle != null)
                    {
                        // Атомарная регистрация: используем TryAddVehicle
                        if (!_vehicleRegistry.TryAddVehicle(regVehicle.VehicleId, regVehicle.BaseNode, regVehicle.ServiceSpots))
                        {
                            _logger.LogWarning("Global vehicle limit reached. Waiting for an available vehicle...");
                            while (true)
                            {
                                vehicleInfo = _vehicleRegistry.AcquireAvailableVehicle(request.AircraftId);
                                if (!string.IsNullOrEmpty(vehicleInfo.VehicleId))
                                {
                                    vehicleId = vehicleInfo.VehicleId;
                                    baseNode = vehicleInfo.BaseNode;
                                    break;
                                }
                                await Task.Delay(1000);
                            }
                        }
                        else
                        {
                            vehicleId = regVehicle.VehicleId;
                            baseNode = regVehicle.BaseNode;
                            _logger.LogInformation("Registered new cleaning vehicle {VehicleId}", vehicleId);
                        }
                    }
                    else
                    {
                        _logger.LogError("Failed to register new cleaning vehicle for AircraftId {AircraftId}", request.AircraftId);
                        DecrementFlightVehicleCount(flightId);
                        return;
                    }
                }
                else
                {
                    // Если уже 5 машин, ждём, пока одна не освободится
                    while (true)
                    {
                        vehicleInfo = _vehicleRegistry.AcquireAvailableVehicle(request.AircraftId);
                        if (!string.IsNullOrEmpty(vehicleInfo.VehicleId))
                        {
                            vehicleId = vehicleInfo.VehicleId;
                            baseNode = vehicleInfo.BaseNode;
                            break;
                        }
                        _logger.LogInformation("Waiting for available cleaning vehicle for AircraftId {AircraftId}", request.AircraftId);
                        await Task.Delay(1000);
                    }
                }
            }
            else
            {
                vehicleId = vehicleInfo.VehicleId;
                baseNode = vehicleInfo.BaseNode;
            }

            _vehicleRegistry.MarkAsBusy(vehicleId);
            _logger.LogInformation("Vehicle {VehicleId} marked as Busy", vehicleId);
            await _hubContext.Clients.All.SendAsync("ReceiveVehicleUpdate", _vehicleRegistry.GetAllVehicles());
            await Task.Yield();

            // Маршрут от базы до точки очистки
            var route = await _groundClient.FetchRouteAsync(baseNode, destination, "cleaning");
            if (route != null)
            {
                _logger.LogInformation("Route to cleaning destination: {Route}", string.Join(" -> ", route));
                for (int j = 0; j < route.Length - 1; j++)
                {
                    var dist = await _groundClient.RequestPermissionAsync(vehicleId, route[j], route[j + 1], "cleaning");
                    if (dist != null)
                    {
                        int delay = (int)Math.Ceiling(dist.Value / VehicleSpeed);
                        _logger.LogInformation("Segment {Index}: delay {Delay} sec", j, delay);
                        await Task.Delay(delay * 1000);
                        await _groundClient.NotifyArrivalAsync(vehicleId, route[j + 1], "cleaning");
                        await _hubContext.Clients.All.SendAsync("ReceiveVehicleUpdate", _vehicleRegistry.GetAllVehicles());
                        await Task.Yield();
                    }
                }
                _logger.LogInformation("Performing cleaning operation on vehicle {VehicleId} with WaterAmount: {Water}", vehicleId, waterForVehicle);
                await Task.Delay(5000);
            }

            var returnRoute = await _groundClient.FetchRouteAsync(destination, baseNode, "cleaning");
            if (returnRoute != null)
            {
                _logger.LogInformation("Return route for cleaning vehicle: {Route}", string.Join(" -> ", returnRoute));
                for (int j = 0; j < returnRoute.Length - 1; j++)
                {
                    var dist = await _groundClient.RequestPermissionAsync(vehicleId, returnRoute[j], returnRoute[j + 1], "cleaning");
                    if (dist != null)
                    {
                        int delay = (int)Math.Ceiling(dist.Value / VehicleSpeed);
                        await Task.Delay(delay * 1000);
                        await _groundClient.NotifyArrivalAsync(vehicleId, returnRoute[j + 1], "cleaning");
                        await _hubContext.Clients.All.SendAsync("ReceiveVehicleUpdate", _vehicleRegistry.GetAllVehicles());
                        await Task.Yield();
                    }
                }
            }

            _vehicleRegistry.MarkAsAvailable(vehicleId, baseNode);
            _logger.LogInformation("Vehicle {VehicleId} marked as Available", vehicleId);
            await _hubContext.Clients.All.SendAsync("ReceiveVehicleUpdate", _vehicleRegistry.GetAllVehicles());
            await Task.Yield();

            DecrementFlightVehicleCount(flightId);
        }
    }
}
