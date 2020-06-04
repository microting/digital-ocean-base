using DigitalOcean.API.Models.Responses;
using Microting.DigitalOceanBase.Infrastructure.Api.Clients.Requests;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microting.DigitalOceanBase.Infrastructure.Api.Clients
{
    public interface IApiClient
    {
        Task<Droplet> CreateDroplet(CreateDropletRequest request);
        Task<List<Droplet>> GetDropletsList();
        Task<List<Image>> GetImagesList();
        void Init(string token);
        Task<Action> RebuildDroplet(long dropletId, long imageId);
    }
}