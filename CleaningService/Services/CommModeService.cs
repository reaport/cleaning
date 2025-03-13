using Microsoft.Extensions.Logging;

namespace CleaningService.Services
{
    public class CommModeService : ICommModeService
    {
        private bool _useMock = false;
        private readonly ILogger<CommModeService> _logger;
        public CommModeService(ILogger<CommModeService> logger)
        {
            _logger = logger;
        }
        public bool UseMock
        {
            get => _useMock;
            set
            {
                _useMock = value;
                _logger.LogInformation("CommModeService: UseMock set to {Value}", value);
            }
        }
    }
}
