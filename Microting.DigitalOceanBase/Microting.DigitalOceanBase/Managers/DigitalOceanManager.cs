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
using System.Threading;

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
            var apiReg = _mapper.Map< List<Region>>(result.Size.Regions);
            var apiTags = _mapper.Map<List<Tag>>(result.Tags);

            await UpdateTags(userId, apiTags);
            await UpdateRegions(userId, apiReg);

            return await CreateDropletRecord(userId, apiDroplet, apiSize, apiTags, apiReg);
        }

        public async Task<Droplet> RebuildDropletAsync(int userId, int dropletId, int imageId)
        {
            // proper droplet resolving
            var droplet = await _dbContext.Droplets
                .Include(t => t.Size.SizeRegions)
                .FirstOrDefaultAsync(t => t.DoUid == dropletId);
            if (droplet == null)
                throw new NullReferenceException("Droplet is not found");

            var image = await _dbContext.Images.FirstOrDefaultAsync(t => t.DoUid == imageId);
            if (image == null)
                throw new NullReferenceException("Image is not found");

            var tags = await _dbContext.Tags
                .Where(t => droplet.DropletTags.Select(m => m.TagId).Contains(t.Id))
                .ToListAsync();

            var regs = await _dbContext.Regions
                .Where(t => droplet.Size.SizeRegions.Select(m => m.RegionId).Contains(t.Id))
                .ToListAsync();

            // requested image

            var response = await _apiClient.GetDroplet(dropletId);
            var apiDroplet = _mapper.Map<Droplet>(response);
            var apiSize = _mapper.Map<Size>(response.Size);
            var apiTags = _mapper.Map<List<Tag>>(response.Tags);
            var apiReg = _mapper.Map<List<Region>>(response.Size.Regions);

            droplet.RequestedImageId = imageId;
            droplet.RequestedImageName = image.Name;
            await UpdateDroplet(userId, apiDroplet, apiTags, apiReg, droplet);


            var result = await _apiClient.RebuildDroplet(dropletId, imageId);
            while (result.Status != "completed")
            {
                Thread.Sleep(5000);
                result = await _apiClient.GetStatus(result.Id);

                if (result.Status == "errored")
                    throw new InvalidOperationException("Failed to rebuild droplet");
            }

            // created image
            var dbDroplet = await _dbContext.Droplets.FirstOrDefaultAsync(t => t.DoUid == dropletId);

            var response2 = await _apiClient.GetDroplet(dropletId);
            var apiDroplet2 = _mapper.Map<Droplet>(response2);
            var apiSize2 = _mapper.Map<Size>(response2.Size);
            var apiTags2 = _mapper.Map<List<Tag>>(response2.Tags);
            var apiReg2 = _mapper.Map<List<Region>>(response2.Size.Regions);

            await UpdateTags(userId, apiTags);
            await UpdateRegions(userId, apiReg);

            return await UpdateDroplet(userId, apiDroplet2, apiTags2, apiReg2, dbDroplet);
        }

        public async Task FetchDropletsAsync(int userId)
        {
            await SyncDropletsInfo(userId);
            await SyncImageInfo(userId);
        }


        private async Task SyncImageInfo(int userId)
        {
            var images = await _apiClient.GetImagesList();
            var apiImages = _mapper.Map<List<Image>>(images);

            var dbImages = await _dbContext.Images
                .Where(t => t.WorkflowState != WorkflowStates.Removed)
                .ToListAsync(); // check correct data loading

            var storedImages = dbImages
                .GroupBy(g => g.DoUid)
                .Select(m => m.OrderByDescending(h => h.Version).First());

            foreach (var item in apiImages)
            {
                // add if was removed
                var dbImage = storedImages.LastOrDefault(t => t.DoUid == item.DoUid);
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
            var droplets = await _apiClient.GetDropletsList();
            
            var dbDroplets = await _dbContext.Droplets
                .Include( t => t.Size)
                .Include(t => t.Size.SizeRegions)
                .Include(t => t.DropletTags)
                .Where(t => t.WorkflowState != WorkflowStates.Removed)
                .ToListAsync();

            var storedDroplets = dbDroplets
                .GroupBy(g => g.DoUid)
                .Select(m => m.OrderByDescending(h => h.Version).First());

            foreach (var d in droplets)
            {
                var apiDroplet = _mapper.Map<Droplet>(d);
                var apiSize = _mapper.Map<Size>(d.Size);
                var apiTags = _mapper.Map<List<Tag>>(d.Tags);
                var apiReg = _mapper.Map<List<Region>>(d.Size.Regions);

                await UpdateTags(userId, apiTags);
                await UpdateRegions(userId, apiReg);

                var dbDroplet = storedDroplets.FirstOrDefault(t => t.DoUid == apiDroplet.DoUid);

                // add new droplet
                if (dbDroplet == null)
                {
                    await CreateDropletRecord(userId, apiDroplet, apiSize, apiTags, apiReg);
                    continue;
                }

                await UpdateDroplet(userId, apiDroplet, apiTags, apiReg, dbDroplet);
            }

            // remove missing droplets
            var apiDroplets = _mapper.Map<List<Droplet>>(droplets);
            foreach (var item in storedDroplets)
                await RemoveDropletRecord(userId, apiDroplets, item);
        }

        private async Task<Droplet> UpdateDroplet(int userId, Droplet apiDroplet, List<Tag> apiTags, List<Region> apiReg, Droplet dbDroplet)
        {
            apiDroplet.DropletTags = apiTags.Select(t => new DropletTag() { Droplet = apiDroplet, Tag = t }).ToList();
            apiDroplet.Size.SizeRegions = apiReg.Select(t => new SizeRegion() { Size = apiDroplet.Size, Region = t }).ToList();

            using (var tran = _dbContext.Database.BeginTransaction())
            {
                var dTags = apiTags.Select(t => new DropletTag() { Droplet = apiDroplet, Tag = t }).ToList();
                var sizeReg = apiReg.Select(t => new SizeRegion() { Size = apiDroplet.Size, Region = t }).ToList();

                apiDroplet.Size.Id = dbDroplet.Size.Id;
                apiDroplet.Size.UpdatedByUserId = userId;
                await apiDroplet.Size.Update<Size>(_dbContext);

                foreach (var sr in sizeReg)
                {
                    var ids = await _dbContext.SizeRegion.Where(g => g.Size.Slug == sr.Size.Slug && g.Region.Name == sr.Region.Name).Select(t => t.Id).ToListAsync();
                    if (ids.Count > 1)
                    {
                        sr.Id = ids.OrderByDescending(t => t).ToList()[1];
                        sr.UpdatedByUserId = userId;
                        await sr.Update<SizeRegion>(_dbContext);
                    }
                    else
                    {
                        sr.CreatedByUserId = userId;
                        await sr.Create(_dbContext);
                    }
                }

                apiDroplet.Id = dbDroplet.Id;
                await apiDroplet.Update<Droplet>(_dbContext);

                foreach (var dt in dTags)
                {
                    var ids = await _dbContext.DropletTag.Where(g => g.Droplet.DoUid == dt.Droplet.DoUid && g.Tag.Name == dt.Tag.Name).Select(t => t.Id).ToListAsync();
                    if (ids.Count > 1)
                    {
                        dt.Id = ids.OrderByDescending(t => t).ToList()[1];
                        dt.UpdatedByUserId = userId;
                        await dt.Update<DropletTag>(_dbContext);
                    }
                    else
                    {
                        dt.CreatedByUserId = userId;
                        await dt.Create(_dbContext);
                    }
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

                    var sizeCopy = _mapper.Map<Size>(item.Size);
                    var sizeReg = item.Size.SizeRegions.Select(t => new SizeRegion() { Size = sizeCopy, Region = t.Region }).ToList();
                    var dTags = item.DropletTags.Select(t => new DropletTag() { Droplet = item, Tag = t.Tag }).ToList();

                    sizeCopy.Droplet = null;
                    sizeCopy.SizeRegions = null;
                    sizeCopy.Id = item.Sizeid;
                    sizeCopy.UpdatedByUserId = userId;
                    await sizeCopy.Delete<Size>(_dbContext);

                    item.Size = sizeCopy;
                    item.UpdatedByUserId = userId;
                    await item.Delete<Droplet>(_dbContext);

                    foreach (var sr in sizeReg)
                    {
                        var ids = await _dbContext.SizeRegion.Where(g => g.Size.Slug == sr.Size.Slug && g.Region.Name == sr.Region.Name).Select(t => t.Id).ToListAsync();
                        if (ids.Count > 1)
                        {
                            sr.Id = ids.OrderByDescending(t => t).ToList()[1];
                            sr.UpdatedByUserId = userId;
                            await sr.Delete<SizeRegion>(_dbContext);
                        }
                    }

                    foreach (var dt in dTags)
                    {
                        var ids = await _dbContext.DropletTag.Where(g => g.Droplet.DoUid == dt.Droplet.DoUid && g.Tag.Name == dt.Tag.Name).Select(t => t.Id).ToListAsync();
                        if (ids.Any())
                        {
                            dt.Droplet.Size = null;
                            dt.Droplet.DropletTags = null;
                            dt.Id = Enumerable.Max(ids);
                            dt.UpdatedByUserId = userId;
                            await dt.Delete<DropletTag>(_dbContext);
                        }
                    }
                }
                await tran.CommitAsync();
            }
        }

        private async Task<Droplet> CreateDropletRecord(int userId, Droplet apiDroplet, Size apiSize, List<Tag> apiTags, List<Region> apiRegions)
        {
            using (var tran = _dbContext.Database.BeginTransaction())
            {
                apiSize.CreatedByUserId = userId;
                await apiSize.Create(_dbContext);

                foreach (var region in apiRegions)
                {
                    var sr = new SizeRegion() { Size = apiSize, Region = region, CreatedByUserId = userId };
                    apiSize.SizeRegions.Add(sr);
                    await sr.Create(_dbContext);
                }

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
                else
                    regions[i] = dbRegion;
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
