using AutoMapper;
using Microting.DigitalOceanBase.Infrastructure.Api.Clients;
using Microting.DigitalOceanBase.Infrastructure.Data;
using Microting.DigitalOceanBase.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microting.DigitalOceanBase.Infrastructure.Api.Clients.Requests;
using Microting.DigitalOceanBase.Infrastructure.Constants;

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

        public async Task FetchDropletsAsync(int userId)
        {
            try
            {
                //await SyncDropletsInfo(userId);
                await SyncImageInfo(userId);
            }
            catch (Exception)
            {

                throw;
            }
           
        }

        public async Task<Droplet> CreateDropletAsync(int userId, CreateDropletRequest request)
        {
            try
            {
                // check key name, or all keys
                var result = await _apiClient.CreateDroplet(request);

                var apiDroplet = _mapper.Map<Droplet>(result);
                var apiSize = _mapper.Map<Size>(result.Size);
                var apiTags = _mapper.Map<List<Tag>>(result.Tags);
                return await CreateDropletRecord(userId, apiDroplet, apiSize, apiTags);
            }
            catch (Exception)
            {

                throw;
            }
            
        }

        public async Task<Droplet> RebuildDropletAsync(int userId, int dropletId, int imageId)
        {
            try
            {
                var droplet = await _dbContext.Droplets.FirstOrDefaultAsync(t => t.DoUid == dropletId);
                if (droplet == null)
                    throw new NullReferenceException("Droplet is not found");

                var image = await _dbContext.Images.FirstOrDefaultAsync(t => t.DoUid == imageId);
                if (image == null)
                    throw new NullReferenceException("Image is not found");

                // requested image

                var result = await _apiClient.RebuildDroplet(dropletId, imageId);
                if (result.Status != "success")
                    throw new InvalidOperationException("Failed to rebuild droplet");

                // created image

                return null;
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
            .ToListAsync();// check correct data loading

            foreach (var item in apiImages)
            {
                // add if was removed
                var dbImage = storedImages.LastOrDefault(t => t.DoUid == item.DoUid && t.WorkflowState != WorkflowStates.Removed);
                if (dbImage == null)
                {
                    item.CreatedByUserId = userId;
                    await item.Create(_dbContext);
                    continue;
                }

                // check update
                item.Id = dbImage.Id;
                item.UpdatedByUserId = userId;
                await item.Update<Image>(_dbContext);
            }

            foreach (var item in storedImages)
            {
                var image = apiImages.LastOrDefault(t => t.DoUid == item.DoUid);
                if (image == null)
                {
                    item.UpdatedByUserId = userId;
                    await item.Delete<Image>(_dbContext);
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
                    await CreateDropletRecord(userId, apiDroplet, apiSize, apiTags);
                    continue;
                }

                //update existing
                apiDroplet.Id = dbDroplet.Id;
                apiDroplet.UpdatedByUserId = userId;
                await apiDroplet.Update<Droplet>(_dbContext);

                apiSize.Id = dbDroplet.Size.Id;
                apiSize.UpdatedByUserId = userId;
                await apiSize.Update<Size>(_dbContext);
            }

            // remove missing droplets
            var apiDroplets = _mapper.Map<List<Droplet>>(droplets);
            foreach (var item in storedDroplets)
                await RemoveDropletRecord(userId, apiDroplets, item);
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
                        await dt.Delete<DropletTag>(_dbContext);
                    }

                    foreach (var reg in item.Size.Regions)
                    {
                        reg.UpdatedByUserId = userId;
                        await reg.Delete<Region>(_dbContext);
                    }

                    item.Size.UpdatedByUserId = userId;
                    await item.Size.Delete<Size>(_dbContext);

                    item.UpdatedByUserId = userId;
                    await item.Delete<Droplet>(_dbContext);
                }
                await tran.CommitAsync();
            }
        }

        private async Task<Droplet> CreateDropletRecord(int userId, Droplet apiDroplet, Size apiSize, List<Tag> apiTags)
        {
            using (var tran = _dbContext.Database.BeginTransaction())
            {
                await AddTags(userId, apiTags);

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

                return apiDroplet;
            }
        }

        private async Task AddTags(int userId, List<Tag> apiTags)
        {
            for (int i = 0; i < apiTags.Count; i++)
            {
                var dbTag = _dbContext.Tags.FirstOrDefault(t => t.Name == apiTags[i].Name && t.WorkflowState != WorkflowStates.Removed);
                if (dbTag == null)
                {
                    apiTags[i].CreatedByUserId = userId;
                    await apiTags[i].Create(_dbContext);
                }
                else
                    apiTags[i] = dbTag;
            }
        }
    }
}
