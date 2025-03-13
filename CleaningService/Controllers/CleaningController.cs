using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using CleaningService.Models;
using CleaningService.Services;

namespace CleaningService.Controllers
{
    [ApiController]
    [Route("")]
    public class CleaningController : ControllerBase
    {
        private readonly ICleaningProcessService _cleaningProcessService;
        private readonly ILogger<CleaningController> _logger;

        public CleaningController(
            ICleaningProcessService cleaningProcessService,
            ILogger<CleaningController> logger)
        {
            _cleaningProcessService = cleaningProcessService;
            _logger = logger;
        }

        [HttpPost("request")]
        public async Task<IActionResult> RequestCleaning([FromBody] RequestCleaningInput request)
        {
            try
            {
                if (request == null || string.IsNullOrEmpty(request.AircraftId) || string.IsNullOrEmpty(request.NodeId))
                {
                    _logger.LogWarning("RequestCleaning: Invalid input parameters.");
                    return BadRequest(new RequestCleaningErrorResponse { errorCode = 100, message = "AircraftId and NodeId are required" });
                }
                if (request.WaterAmount < 0)
                {
                    _logger.LogWarning("RequestCleaning: WaterAmount must be non-negative.");
                    return BadRequest(new RequestCleaningErrorResponse { errorCode = 101, message = "WaterAmount must be a non-negative integer" });
                }

                var response = await _cleaningProcessService.ProcessCleaningRequest(request);
                return Ok(response);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "RequestCleaning: Invalid input.");
                return BadRequest(new RequestCleaningErrorResponse { errorCode = 100, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RequestCleaning: Internal server error.");
                return StatusCode(500, new RequestCleaningErrorResponse { errorCode = 500, message = "InternalServerError" });
            }
        }
    }
}
