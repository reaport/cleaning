using CleaningService.Models;

namespace CleaningService.Services
{
    public interface IAdminConfigService
    {
        AdminConfig GetConfig();
        void UpdateConfig(AdminConfig config);
    }
}
