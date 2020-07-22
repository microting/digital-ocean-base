using AutoMapper;
using DigitalOcean.API;
using DigitalOcean.API.Models.Responses;
using Microting.DigitalOceanBase.Infrastructure.Api.Clients.Requests;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microting.DigitalOceanBase.Infrastructure.Api.Clients
{
    internal class ApiClient : IApiClient
    {
        private DigitalOceanClient _doClient;
        private readonly IMapper _mapper;

        public ApiClient(IMapper mapper, string token)
        {
            _mapper = mapper;

            _doClient = new DigitalOceanClient(token);
        }
        
        public async Task<List<Droplet>> GetDropletsList()
        {
            var droplets = await _doClient.Droplets.GetAll();

            return droplets.Where(t => t.Status == "active").ToList();
        }

        public async Task<List<Image>> GetImagesList()
        {
            var images = await _doClient.Images.GetAll(DigitalOcean.API.Models.Requests.ImageType.All);

            return images.ToList();
        }

        public async Task<Droplet> GetDroplet(int dropletId)
        {
            return await _doClient.Droplets.Get(dropletId);
        }


        public async Task<Action> RebuildDroplet(long dropletId, long imageId)
        {
            var result = await _doClient.DropletActions.Rebuild(dropletId, imageId);
            return result;
        }

        public async Task<Droplet> CreateDroplet(CreateDropletRequest request)
        {
            var doRequest = _mapper.Map<CreateDropletRequest, DigitalOcean.API.Models.Requests.Droplet>(request);

            if (request.SshKeys == null)
            {
                var keys = await _doClient.Keys.GetAll();
                if (keys != null)
                    doRequest.SshKeys = keys.Select(t => (object)t.Id).ToList();
            }

            var result = await _doClient.Droplets.Create(doRequest);

            return result;
        }

        public async Task<Action> GetStatus(long actionId)
        {
            return await _doClient.Actions.Get(actionId);
        }

    }
}
