using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace CleaningService.Hubs
{
    public class VehicleStatusHub : Hub
    {
        public async Task SendUpdate(object data)
        {
            await Clients.All.SendAsync("ReceiveVehicleUpdate", data);
        }
    }
}
