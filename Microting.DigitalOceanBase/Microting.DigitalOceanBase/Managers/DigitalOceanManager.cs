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
            try
            {
                // await SyncDropletsInfo(userId);
                await SyncImageInfo(userId);
            }
            catch (Exception)
            {

                throw;
            }
           
        }

        private async Task SyncImageInfo(int userId)
        {
            var images = await _apiClient.GetImagesList();
            var apiImages = _mapper.Map<List<Image>>(images);

            var storedImages = await _dbContext.Images
            .ToListAsync();

            foreach (var item in apiImages)
            {
                var dbImage = storedImages.FirstOrDefault(t => t.DoUid == item.DoUid);
                if (dbImage == null)
                {
                    item.CreatedByUserId = userId;
                    await item.Create(_dbContext);
                    continue;
                }

                // check update
                item.Id = dbImage.Id;
                item.UpdatedByUserId = userId;
                await item.Update(_dbContext);
            }

            foreach (var item in storedImages)
            {
                var image = apiImages.FirstOrDefault(t => t.DoUid == item.DoUid);
                if (image == null)
                {
                    item.UpdatedByUserId = userId;
                    await image.Delete(_dbContext);
                }
                    
            }
        }

        private async Task SyncDropletsInfo(int userId)
        {
            // todo: tag, region already exists during creation
            var droplets = await _apiClient.GetDropletsList();
            var storedDroplets = await _dbContext.Droplets
                .Include(t => t.Size.Regions)
                .ToListAsync();

            foreach (var d in droplets)
            {
                var apiDroplet = _mapper.Map<Droplet>(d);
                var apiSize = _mapper.Map<Size>(d.Size);
                var apiTags = _mapper.Map<List<Tag>>(d.Tags);
                var dbDroplet = storedDroplets.FirstOrDefault(t => t.DoUid == apiDroplet.DoUid);

                // add new droplet
                if (dbDroplet == null)
                {
                    await CreateDropletRecord(_dbContext, userId, apiDroplet, apiSize, apiTags);
                    continue;
                }

                //update existing
                //apiDroplet.Id = dbDroplet.Id;
                //apiDroplet.UpdatedByUserId = userId;
                //await apiDroplet.Update(_dbContext);

                //apiSize.Id = dbDroplet.Size.Id;
                //apiSize.UpdatedByUserId = userId;
                //await apiDroplet.Update(_dbContext);
            }

            // remove missing droplets
            //var apiDroplets = _mapper.Map<List<Droplet>>(droplets);
            //foreach (var item in storedDroplets)
            //    await RemoveDropletRecord(userId, apiDroplets, item);
        }

        private async Task RemoveDropletRecord(int userId, List<Droplet> apiDroplets, Droplet item)
        {
            using (var tran = _dbContext.Database.BeginTransaction())
            {
                var droplet = apiDroplets.FirstOrDefault(t => t.DoUid == item.DoUid);
                if (droplet == null)
                {
                    foreach (var dt in item.DropletTags)
                    {
                        dt.UpdatedByUserId = userId;
                        await dt.Delete(_dbContext);
                    }

                    foreach (var reg in item.Size.Regions)
                    {
                        reg.UpdatedByUserId = userId;
                        await reg.Delete(_dbContext);
                    }

                    item.Size.UpdatedByUserId = userId;
                    await item.Size.Delete(_dbContext);

                    item.UpdatedByUserId = userId;
                    await item.Delete(_dbContext);
                }
                await tran.CommitAsync();
            }
        }

        private async Task CreateDropletRecord(DigitalOceanDbContext dbContext, int userId, Droplet apiDroplet, Size apiSize, List<Tag> apiTags)
        {
            using (var tran = _dbContext.Database.BeginTransaction())
            {
                foreach (var tag in apiTags)
                {
                    tag.CreatedByUserId = userId;
                    await tag.Create(_dbContext);
                }

                foreach (var reg in apiSize.Regions)
                {
                    reg.CreatedByUserId = userId;
                    await reg.Create(_dbContext);
                }

                apiSize.Regions.AddRange(apiSize.Regions);

                apiSize.CreatedByUserId = userId;
                await apiSize.Create(_dbContext);

                apiDroplet.Size = apiSize;
                apiDroplet.CreatedByUserId = userId;
                await apiDroplet.Create(_dbContext);

                foreach (var tag in apiTags)
                {
                    var dt = new DropletTag() { Droplet = apiDroplet, Tag = tag, CreatedByUserId = userId };
                    apiDroplet.DropletTags.Add(dt);
                    await dt.Create(_dbContext);
                }
              await tran.CommitAsync();
            }
        }
    }
}
