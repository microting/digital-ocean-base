using AutoMapper;
using Microting.DigitalOceanBase.Infrastructure.Api.Clients;
using Microting.DigitalOceanBase.Infrastructure.Data;
using Microting.DigitalOceanBase.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microting.DigitalOceanBase.Managers
{
    public class DigitalOceanManager : IDigitalOceanManager
    {
        private readonly DigitalOceanDbContext _dbContext;
        private readonly IApiClient _apiClient;
        private readonly IMapper _mapper;

        public DigitalOceanManager(DigitalOceanDbContext dbContext, IApiClient apiClient, IMapper mapper)
        {
            _dbContext = dbContext;
            _apiClient = apiClient;
            _mapper = mapper;

            var cred = _dbContext.PluginConfigurationValues?.FirstOrDefault(t => t.Name == "DigitalOceanBaseSettings:DigitalOceanToken");

            #if DEBUG
                if (cred == null)
                    cred = new PluginConfigurationValues() { Name = "DigitalOceanBaseSettings:DigitalOceanToken", Value = "c50fda0739cbc83182d96f4d07287b0ccfb19474dc0f1b7a387a7a546a5da11d" };
            #endif


            if (cred == null)
                throw new NullReferenceException("DigitalOcean token is not found");

            _apiClient.Init(cred.Value);
        }   

        public async Task FetchDroplets(int userId)
        {
            // todo: tags, regions
            var droplets = await _apiClient.GetDropletsList();
            var storedDroplets = await _dbContext.Droplets.ToListAsync();

            foreach (var d in droplets)
            {
                var apiDroplet = _mapper.Map<Droplet>(d);
                var apiSize = _mapper.Map<Size>(d.Size);
                var dbDroplet = storedDroplets.FirstOrDefault(t => t.DoUid == apiDroplet.DoUid);

                // add new droplet
                if (dbDroplet == null)
                {
                    apiDroplet.CreatedByUserId = userId;
                    await apiDroplet.Create(_dbContext);

                    apiSize.CreatedByUserId = userId;
                    await apiSize.Create(_dbContext);
                    continue;
                }

                // update existing
                apiDroplet.Id = dbDroplet.Id;
                apiDroplet.UpdatedByUserId = userId;
                await apiDroplet.Update(_dbContext);

                apiSize.Id = dbDroplet.Size.Id;
                apiSize.UpdatedByUserId = userId;
                await apiDroplet.Update(_dbContext);
            }

            // remove missing droplets
            var apiDroplets = _mapper.Map<List<Droplet>>(droplets);
            foreach (var item in storedDroplets)
            {
                var droplet = apiDroplets.FirstOrDefault(t => t.DoUid == item.DoUid);
                if (droplet == null)
                {
                    item.UpdatedByUserId = userId;
                    await item.Delete(_dbContext);

                    item.Size.UpdatedByUserId = userId;
                    await item.Size.Delete(_dbContext);
                }
            }
        }
    }
}
