using Microsoft.Extensions.Logging;

namespace CleaningService.Services
{
    public class CommModeService : ICommModeService
    {
        private readonly ILogger<CommModeService> _logger;
        private bool _useMock = true; // По умолчанию используется Mock
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
