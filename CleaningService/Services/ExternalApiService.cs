using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using CleaningService.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace CleaningService.Services
{
    public class ExternalApiService : IExternalApiService
    {
        private readonly HttpClient _externalApiClient;
        private readonly HttpClient _orchestratorClient;
        private readonly ILogger<ExternalApiService> _logger;
        private readonly ICommModeService _commModeService;
        private const int MaxRetries = 3;

        public ExternalApiService(IHttpClientFactory httpClientFactory, ILogger<ExternalApiService> logger, ICommModeService commModeService)
        {
            _externalApiClient = httpClientFactory.CreateClient("ExternalApi");
            _orchestratorClient = httpClientFactory.CreateClient("Orchestrator");
            _logger = logger;
            _commModeService = commModeService;
        }

        public async Task<RegisterVehicleResponse> RegisterVehicleAsync(string type)
        {
            if (_commModeService.UseMock)
            {
                _logger.LogInformation("MOCK: Registering cleaning vehicle of type {Type}", type);
                await Task.Delay(200);
                return new RegisterVehicleResponse
                {
                    VehicleId = $"cleaning_{Guid.NewGuid().ToString().Substring(0, 8)}",
                    GarageNodeId = "garrage_cleaning_1",
                    ServiceSpots = new Dictionary<string, string>
                    {
                        { "parking_1", "parking_1_cleaning_1" },
                        { "parking_2", "parking_2_cleaning_1" }
                    }
                };
            }

            _logger.LogInformation("Sending request to register vehicle of type: {Type}", type);
            var response = await _externalApiClient.PostAsJsonAsync<object>($"/register-vehicle/{type}", null);
            _logger.LogInformation("Received registration response, status: {StatusCode}", response.StatusCode);
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<RegisterVehicleResponse>();
            string responseBody = JsonSerializer.Serialize(result);
            _logger.LogInformation("Registration response: {ResponseBody}", responseBody);
            return result;
        }

        public async Task<List<string>> GetRouteAsync(string from, string to, string vehicleType)
        {
            if (_commModeService.UseMock)
            {
                _logger.LogInformation("MOCK: Returning fake route from {From} to {To}", from, to);
                await Task.Delay(100);
                return new List<string> { from, "MockIntermediate", to };
            }

            var payload = new { from, to, type = vehicleType };
            string requestBody = JsonConvert.SerializeObject(payload, Formatting.Indented);
            _logger.LogInformation("Sending route request:\n{RequestBody}", requestBody);

            var response = await _externalApiClient.PostAsJsonAsync<object>("/route", payload);
            _logger.LogInformation("Received route response, status: {StatusCode}", response.StatusCode);
            response.EnsureSuccessStatusCode();
            var nodes = await response.Content.ReadFromJsonAsync<List<string>>();
            if (nodes == null || nodes.Count < 2)
                throw new Exception("Invalid route format or not enough nodes.");
            _logger.LogInformation("Received route from {From} to {To}: {Route}",
                from, to, string.Join(" -> ", nodes));
            return nodes;
        }

        public async Task<double> RequestMoveAsync(string vehicleId, string vehicleType, string from, string to)
        {
            if (_commModeService.UseMock)
            {
                _logger.LogInformation("MOCK: Returning fake distance for vehicle {VehicleId}", vehicleId);
                await Task.Delay(100);
                return 50.0;
            }

            var payload = new { vehicleId, vehicleType, from, to };
            string requestBody = JsonSerializer.Serialize(payload);
            _logger.LogInformation("Sending move request:\n{RequestBody}", requestBody);

            HttpResponseMessage response = null;
            int retryCount = 0;
            while (true)
            {
                response = await _externalApiClient.PostAsJsonAsync("/move", payload);
                _logger.LogInformation("Received move response, status: {StatusCode}", response.StatusCode);

                if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
                {
                    _logger.LogWarning("Conflict encountered for move request from {From} to {To} for vehicle {VehicleId}. Retrying...", from, to, vehicleId);
                    await Task.Delay(TimeSpan.FromSeconds(2));
                    retryCount++;
                    if (retryCount >= 30)
                    {
                        _logger.LogError("Exceeded maximum retries for move request from {From} to {To} for vehicle {VehicleId}.", from, to, vehicleId);
                        break;
                    }
                    continue;
                }
                break;
            }

            response.EnsureSuccessStatusCode();
            var moveResponse = await response.Content.ReadFromJsonAsync<MoveResponse>();
            string respBody = JsonSerializer.Serialize(moveResponse);
            _logger.LogInformation("Move response: {ResponseBody}", respBody);
            return moveResponse.Distance;
        }

        public async Task NotifyArrivalAsync(string vehicleId, string vehicleType, string nodeId)
        {
            if (_commModeService.UseMock)
            {
                _logger.LogInformation("MOCK: Notifying arrival of vehicle {VehicleId} at node {NodeId}", vehicleId, nodeId);
                await Task.Delay(50);
                return;
            }

            var payload = new { vehicleId, vehicleType, nodeId };
            string requestBody = JsonSerializer.Serialize(payload);
            _logger.LogInformation("Sending arrival notification:\n{RequestBody}", requestBody);

            var response = await _externalApiClient.PostAsJsonAsync<object>("/arrived", payload);
            _logger.LogInformation("Received arrival notification response, status: {StatusCode}", response.StatusCode);
            response.EnsureSuccessStatusCode();
        }

        public async Task NotifyCleaningStartAsync(string flightId)
        {
            if (_commModeService.UseMock)
            {
                _logger.LogInformation("MOCK: Notifying cleaning start for flight {FlightId}", flightId);
                await Task.Delay(100);
                return;
            }

            var payload = new { aircraft_id = flightId };
            string requestBody = JsonSerializer.Serialize(payload);
            _logger.LogInformation("Sending cleaning start notification:\n{RequestBody}", requestBody);

            int retryCount = 0;
            while (retryCount < MaxRetries)
            {
                try
                {
                    var response = await _orchestratorClient.PostAsJsonAsync<object>("/cleaning/start", payload);
                    _logger.LogInformation("Received cleaning start response, status: {StatusCode}", response.StatusCode);
                    response.EnsureSuccessStatusCode();
                    return;
                }
                catch (Exception ex)
                {
                    retryCount++;
                    _logger.LogWarning(ex, "Error notifying cleaning start for flight {FlightId}, attempt {Attempt}", flightId, retryCount);
                    await Task.Delay(TimeSpan.FromSeconds(2));
                }
            }
            _logger.LogError("Failed to notify cleaning start for flight {FlightId} after {Retries} attempts. Proceeding without notification.", flightId, MaxRetries);
        }

        public async Task NotifyCleaningFinishAsync(string flightId, int waterAmount)
        {
            if (_commModeService.UseMock)
            {
                _logger.LogInformation("MOCK: Notifying cleaning finish for flight {FlightId} with water amount: {WaterAmount}", flightId, waterAmount);
                await Task.Delay(100);
                return;
            }

            var payload = new { aircraft_id = flightId, water_amount = waterAmount };
            string requestBody = JsonSerializer.Serialize(payload);
            _logger.LogInformation("Sending cleaning finish notification:\n{RequestBody}", requestBody);

            int retryCount = 0;
            while (retryCount < MaxRetries)
            {
                try
                {
                    var response = await _orchestratorClient.PostAsJsonAsync<object>("/cleaning/finish", payload);
                    _logger.LogInformation("Received cleaning finish response, status: {StatusCode}", response.StatusCode);
                    response.EnsureSuccessStatusCode();
                    return;
                }
                catch (Exception ex)
                {
                    retryCount++;
                    _logger.LogWarning(ex, "Error notifying cleaning finish for flight {FlightId}, attempt {Attempt}", flightId, retryCount);
                    await Task.Delay(TimeSpan.FromSeconds(2));
                }
            }
            _logger.LogError("Failed to notify cleaning finish for flight {FlightId} after {Retries} attempts. Proceeding without notification.", flightId, MaxRetries);
        }
    }
}
