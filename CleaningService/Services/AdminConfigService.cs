using CleaningService.Models;

namespace CleaningService.Services
{
    public class AdminConfigService : IAdminConfigService
    {
        private AdminConfig _config = new AdminConfig();
        public AdminConfig GetConfig() => _config;
        public void UpdateConfig(AdminConfig config)
        {
            _config.ConflictRetryCount = config.ConflictRetryCount;
            _config.NumberOfCleaningVehicles = config.NumberOfCleaningVehicles;
        }
    }
}
