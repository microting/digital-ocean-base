using Microting.DigitalOceanBase.Infrastructure.Api.Clients.Requests;
using Microting.DigitalOceanBase.Infrastructure.Data.Entities;
using System.Threading.Tasks;

namespace Microting.DigitalOceanBase.Managers
{
    public interface IDigitalOceanManager
    {
        Task<Droplet> CreateDropletAsync(int userId, CreateDropletRequest request);
        Task FetchDropletsAsync(int userId);
        Task<Droplet> RebuildDropletAsync(int userId, int dropletId, int imageId);
    }
}