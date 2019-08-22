using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace ImageUploadDemo.Hubs
{
    public class ImageHub : Hub
    {
        public async Task SendMessage(string msg)
        {
            await Clients.All.SendAsync("UploadedImage", msg);
        }
    }
}
