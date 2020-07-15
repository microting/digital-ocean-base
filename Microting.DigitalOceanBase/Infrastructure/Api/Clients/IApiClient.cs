using DigitalOcean.API.Models.Responses;
using Microting.DigitalOceanBase.Infrastructure.Api.Clients.Requests;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microting.DigitalOceanBase.Infrastructure.Api.Clients
{
    public interface IApiClient
    {
        Task<Droplet> CreateDroplet(CreateDropletRequest request);
        Task<Droplet> GetDroplet(int dropletId);
        Task<List<Droplet>> GetDropletsList();
        Task<List<Image>> GetImagesList();
        Task<Action> GetStatus(long actionId);
        void Init(string token);
        Task<Action> RebuildDroplet(long dropletId, long imageId);
    }
}