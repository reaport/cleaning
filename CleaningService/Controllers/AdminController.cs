using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CleaningService.Models;
using CleaningService.Services;
using Newtonsoft.Json;

namespace CleaningService.Controllers
{
    public class AdminController : Controller
    {
        private readonly ICapacityService _capacityService;
        private readonly ICommModeService _commModeService;
        private readonly ICleaningProcessService _cleaningProcessService;
        private readonly IVehicleRegistry _vehicleRegistry;
        private readonly ILogger<AdminController> _logger;

        public AdminController(
            ICapacityService capacityService,
            ICommModeService commModeService,
            ICleaningProcessService cleaningProcessService,
            IVehicleRegistry vehicleRegistry,
            ILogger<AdminController> logger)
        {
            _capacityService = capacityService;
            _commModeService = commModeService;
            _cleaningProcessService = cleaningProcessService;
            _vehicleRegistry = vehicleRegistry;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Index()
        {
            var vehicles = _vehicleRegistry.GetAllVehicles();
            var model = new AdminDashboardViewModel
            {
                WaterCapacity = _capacityService.GetCapacity(),
                ModuleMode = _commModeService.UseMock ? "Mock" : "Real",
                Vehicles = new List<CleaningVehicleStatusInfo>(vehicles)
            };
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Index(
            string actionType,
            int? newCapacity,
            string aircraftId,
            string nodeId,
            int? waterAmount,
            bool? useMock)
        {
            try
            {
                switch (actionType)
                {
                    case "ToggleCommMode":
                        if (useMock.HasValue)
                        {
                            _commModeService.UseMock = useMock.Value;
                            _logger.LogInformation("Admin set UseMock={UseMock}", useMock.Value);
                            TempData["Message"] = "Communication mode updated.";
                        }
                        break;

                    case "UpdateCapacity":
                        if (!newCapacity.HasValue || newCapacity < 0)
                        {
                            TempData["Error"] = "Invalid water capacity value.";
                        }
                        else
                        {
                            _capacityService.UpdateCapacity(newCapacity.Value);
                            _logger.LogInformation("Admin updated water capacity to {Capacity}", newCapacity.Value);
                            TempData["Message"] = "Water capacity updated.";
                        }
                        break;

                    case "RequestCleaning":
                        try
                        {
                            if (!waterAmount.HasValue)
                                throw new ArgumentException("WaterAmount is required");
                            var request = new RequestCleaningInput
                            {
                                AircraftId = aircraftId,
                                NodeId = nodeId,
                                WaterAmount = waterAmount.Value
                            };
                            // Запускаем процесс в фоне (через AJAX)
                            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                            {
                                Task.Run(() => _cleaningProcessService.ProcessCleaningRequest(request));
                                return Json(new { message = "Cleaning request started. Updates will appear in real time." });
                            }
                            else
                            {
                                Task.Run(() => _cleaningProcessService.ProcessCleaningRequest(request));
                                TempData["Message"] = "Cleaning request started.";
                            }
                        }
                        catch (Exception ex)
                        {
                            TempData["Error"] = "Error processing cleaning request: " + ex.Message;
                            _logger.LogError(ex, "Error in RequestCleaning");
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Internal server error.";
                _logger.LogError(ex, "Error in AdminController POST");
            }

            return RedirectToAction("Index");
        }
    }
}
