using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using CleaningService.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace CleaningService.Services
{
    public class GroundControlClient : IGroundControlClient
    {
        private readonly HttpClient _client;
        private readonly ILogger<GroundControlClient> _logger;
        private readonly ICommModeService _commMode;

        public GroundControlClient(IHttpClientFactory httpClientFactory, ILogger<GroundControlClient> logger, ICommModeService commMode)
        {
            _client = httpClientFactory.CreateClient();
            _logger = logger;
            _commMode = commMode;
        }

        public async Task<CleaningVehicleStatusInfo> RegisterVehicleAsync(string vehicleType)
        {
            if (_commMode.UseMock)
            {
                _logger.LogInformation("RegisterVehicleAsync (MOCK) for {VehicleType}", vehicleType);
                await Task.Delay(200);
                return new CleaningVehicleStatusInfo
                {
                    VehicleId = $"CleanVehicle-{Guid.NewGuid()}",
                    BaseNode = "CleaningGarage-1",
                    Status = "Busy",
                    ServiceSpots = new System.Collections.Generic.Dictionary<string, string>
                    {
                        { "A123", "CleanNode-A123" },
                        { "B456", "CleanNode-B456" }
                    }
                };
            }
            else
            {
                string url = $"https://ground-control.reaport.ru/register-vehicle/{vehicleType}";
                _logger.LogInformation("RegisterVehicleAsync (REAL) calling {Url}", url);
                var response = await _client.PostAsync(url, null);
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Vehicle registered (REAL) successfully");
                    return await response.Content.ReadFromJsonAsync<CleaningVehicleStatusInfo>();
                }
                _logger.LogWarning("Vehicle registration (REAL) failed with status {StatusCode}", response.StatusCode);
                throw new Exception("Vehicle registration failed");
            }
        }

        public async Task<string[]?> FetchRouteAsync(string from, string to, string type)
        {
            if (_commMode.UseMock)
            {
                _logger.LogInformation("FetchRouteAsync (MOCK) from {From} to {To}", from, to);
                await Task.Delay(200);
                return new string[] { from, "Intermediate1", "Intermediate2", to };
            }
            else
            {
                _logger.LogInformation("FetchRouteAsync (REAL) from {From} to {To}", from, to);
                var requestBody = new { from, to, type };
                var json = JsonConvert.SerializeObject(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _client.PostAsync("https://ground-control.reaport.ru/route", content);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation("Route (REAL): {Result}", result);
                    return JsonConvert.DeserializeObject<string[]>(result);
                }
                _logger.LogWarning("FetchRouteAsync (REAL) failed with status {StatusCode}", response.StatusCode);
                return null;
            }
        }

        public async Task<double?> RequestPermissionAsync(string vehicleId, string from, string to, string vehicleType)
        {
            if (_commMode.UseMock)
            {
                _logger.LogInformation("RequestPermissionAsync (MOCK) for vehicle {VehicleId}", vehicleId);
                await Task.Delay(100);
                return 50.0;
            }
            else
            {
                _logger.LogInformation("RequestPermissionAsync (REAL) for vehicle {VehicleId}", vehicleId);
                var payload = new { vehicleId, vehicleType, from, to };
                var json = JsonConvert.SerializeObject(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _client.PostAsync("https://ground-control.reaport.ru/move", content);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadAsStringAsync();
                    var move = JsonConvert.DeserializeObject<CleaningMoveResponse>(result);
                    return move.Distance;
                }
                _logger.LogWarning("RequestPermissionAsync (REAL) failed with status {StatusCode}", response.StatusCode);
                return null;
            }
        }

        public async Task NotifyArrivalAsync(string vehicleId, string nodeId, string vehicleType)
        {
            if (_commMode.UseMock)
            {
                _logger.LogInformation("NotifyArrivalAsync (MOCK): vehicle {VehicleId} at node {NodeId}", vehicleId, nodeId);
                await Task.Delay(50);
            }
            else
            {
                _logger.LogInformation("NotifyArrivalAsync (REAL): vehicle {VehicleId} at node {NodeId}", vehicleId, nodeId);
                var payload = new { vehicleId, vehicleType, nodeId };
                var json = JsonConvert.SerializeObject(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _client.PostAsync("https://ground-control.reaport.ru/arrived", content);
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("NotifyArrivalAsync (REAL): vehicle {VehicleId} arrived at {NodeId}", vehicleId, nodeId);
                }
                else
                {
                    _logger.LogWarning("NotifyArrivalAsync (REAL) failed with status {StatusCode}", response.StatusCode);
                }
            }
        }
    }

    // Дополнительная модель для десериализации расстояния в REAL-режиме
    public class CleaningMoveResponse
    {
        public double Distance { get; set; }
    }
}
