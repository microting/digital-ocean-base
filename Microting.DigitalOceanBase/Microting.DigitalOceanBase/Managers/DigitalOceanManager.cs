using Microting.DigitalOceanBase.Infrastructure.Api.Clients;
using Microting.DigitalOceanBase.Infrastructure.Data;
using Microting.DigitalOceanBase.Infrastructure.Data.Entities;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Microting.DigitalOceanBase.Managers
{
    public class DigitalOceanManager : IDigitalOceanManager
    {
        private readonly DigitalOceanDbContext _dbContext;
        private readonly IApiClient _apiClient;

        public DigitalOceanManager(DigitalOceanDbContext dbContext, IApiClient apiClient)
        {
            _dbContext = dbContext;
            _apiClient = apiClient;

            var cred = _dbContext.Credentials?.FirstOrDefault(t => t.Key == "DigitalOcean");

            #if DEBUG
                if (cred == null)
                    cred = new Credential() { Key = "DigitalOcean", Value = "c50fda0739cbc83182d96f4d07287b0ccfb19474dc0f1b7a387a7a546a5da11d" };
            #endif


            if (cred == null)
                throw new NullReferenceException("DigitalOcean credential is not found");

            _apiClient.Init(cred.Value);
        }   

        // nuget author, publish command

        public async Task GetDroplets()
        {
           var droplets = await _apiClient.GetDropletsList();

        }
    }
}
