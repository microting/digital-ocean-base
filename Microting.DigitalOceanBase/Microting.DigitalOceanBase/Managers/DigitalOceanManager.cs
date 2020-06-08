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
            if (cred == null)
                throw new NullReferenceException("DigitalOcean token is not found");

            _apiClient.Init(cred.Value);
        }


        public async Task<Droplet> CreateDropletAsync(int userId, CreateDropletRequest request)
        {
            // check key name, or all keys
            var result = await _apiClient.CreateDroplet(request);

            var apiDroplet = _mapper.Map<Droplet>(result);
            var apiSize = _mapper.Map<Size>(result.Size);
            var apiTags = _mapper.Map<List<Tag>>(result.Tags);
            return await CreateDropletRecord(userId, apiDroplet, apiSize, apiTags);
        }

        public async Task<Droplet> RebuildDropletAsync(int userId, int dropletId, int imageId)
        {
            var droplet = await _dbContext.Droplets.FirstOrDefaultAsync(t => t.DoUid == dropletId);
            if (droplet == null)
                throw new NullReferenceException("Droplet is not found");

            var image = await _dbContext.Images.FirstOrDefaultAsync(t => t.DoUid == imageId);
            if (image == null)
                throw new NullReferenceException("Image is not found");

            var tags = await _dbContext.Tags
                .Where(t => droplet.DropletTags.Select(m => m.TagId).Contains(t.Id))
                .ToListAsync();
                
            // requested image
            droplet.RequestedImageId = imageId;
            droplet.RequestedImageName = image.Name;
            await UpdateDroplet(userId, droplet, droplet.Size, tags);


            var result = await _apiClient.RebuildDroplet(dropletId, imageId);
            if (result.Status != "success")
                throw new InvalidOperationException("Failed to rebuild droplet");

            // created image
            var response = await _apiClient.GetDroplet(dropletId);
            var apiDroplet = _mapper.Map<Droplet>(response);
            var apiSize = _mapper.Map<Size>(response.Size);
            var apiTags = _mapper.Map<List<Tag>>(response.Tags);

            await UpdateTags(userId, apiTags);
            await UpdateRegions(userId, apiSize.Regions);
            return await UpdateDroplet(userId, apiDroplet, apiSize, apiTags);
        }

        public async Task FetchDropletsAsync(int userId)
        {
           // await SyncDropletsInfo(userId);
            await SyncImageInfo(userId);
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
            }1
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

                await UpdateTags(userId, apiTags);
                await UpdateRegions(userId, apiSize.Regions);

                // add new droplet
                if (dbDroplet == null)
                {
                    await CreateDropletRecord(userId, apiDroplet, apiSize, apiTags);
                    continue;
                }

                await UpdateDroplet(userId, apiDroplet, apiSize, apiTags);
            }

            // remove missing droplets
            var apiDroplets = _mapper.Map<List<Droplet>>(droplets);
            foreach (var item in storedDroplets)
                await RemoveDropletRecord(userId, apiDroplets, item);
        }

        private async Task<Droplet> UpdateDroplet(int userId, Droplet apiDroplet, Size apiSize, List<Tag> apiTags)
        {
            using (var tran = _dbContext.Database.BeginTransaction())
            {
                apiSize.UpdatedByUserId = userId;
                await apiSize.Update<Size>(_dbContext);

                apiDroplet.Size = apiSize;
                apiDroplet.UpdatedByUserId = userId;
                await apiDroplet.Update<Droplet>(_dbContext);

                apiDroplet.DropletTags = new List<DropletTag>();
                foreach (var tag in apiTags)
                {
                    var dt = new DropletTag() { Droplet = apiDroplet, Tag = tag, UpdatedByUserId = userId };
                    apiDroplet.DropletTags.Add(dt);
                    await dt.Update<DropletTag>(_dbContext);
                }

                await tran.CommitAsync();

                return apiDroplet;
            }
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

        private async Task UpdateRegions(int userId, List<Region> regions)
        {
            for (int i = 0; i < regions.Count; i++)
            {
                var dbRegion = _dbContext.Regions.FirstOrDefault(t => t.Name == regions[i].Name && t.WorkflowState != WorkflowStates.Removed);
                if (dbRegion == null)
                {
                    regions[i].CreatedByUserId = userId;
                    await regions[i].Create(_dbContext);
                }
            }
        }

        private async Task UpdateTags(int userId, List<Tag> apiTags)
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
