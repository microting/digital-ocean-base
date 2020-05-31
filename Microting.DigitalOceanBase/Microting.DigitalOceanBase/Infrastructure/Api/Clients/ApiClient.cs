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

        public ApiClient(IMapper mapper)
        {
            
            _mapper = mapper;
        }
        
        public void Init(string token)
        {
            _doClient = new DigitalOceanClient(token);
        }

        public async Task<List<Droplet>> GetDropletsList()
        {
            // ensure droplet is running
            var droplets = await _doClient.Droplets.GetAll();
            //collect public and private IP for each droplet.
            //var droplet = droplets.First().Networks.V4[0].

            //collect which tags the droplets have.
            //var droplet = droplets.First().Tags
            return droplets.ToList();
        }

        public async Task<List<Image>> GetImagesList()
        {
            // list all avaliable images across our account
            var images = await _doClient.Images.GetAll();

            return images.ToList();
        }


        public async Task<Action> RebuildDroplet(long dropletId, long imageId)
        {
            // - be able to rebuild droplets with a named image.
            var result = await _doClient.DropletActions.Rebuild(dropletId, imageId);

            return result;
        }

        public async Task<Droplet> CreateDroplet(CreateDropletRequest request)
        {
            // be able to trigger creation of a new droplet, with user data and which image to use, data center to use etc.
            var doRequest = _mapper.Map<CreateDropletRequest, DigitalOcean.API.Models.Requests.Droplet>(request);
            var result = await _doClient.Droplets.Create(doRequest);

            return result;
        }

    }
}
