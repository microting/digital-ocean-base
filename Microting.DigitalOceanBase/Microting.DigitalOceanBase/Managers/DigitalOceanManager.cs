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
            var apiTags = _mapper.Map<List<Tag>>(response.Tags);
            var apiReg = _mapper.Map<List<Region>>(response.Size.Regions);

            apiDroplet.RequestedImageId = imageId;
            apiDroplet.RequestedImageName = image.Name;
            await UpdateDroplet(userId, apiDroplet, droplet);


            var result = await _apiClient.RebuildDroplet(dropletId, imageId);
            while (result.Status != "completed")
            {
                Thread.Sleep(5000);
                result = await _apiClient.GetStatus(result.Id);

                if (result.Status == "errored")
                    throw new InvalidOperationException("Failed to rebuild droplet");
            }

            // created image
            var dbDroplets = await _dbContext.Droplets.Where(t => t.DoUid == dropletId).ToListAsync();
            var dbDroplet = dbDroplets.OrderBy(t => t.Id).LastOrDefault();

            var response2 = await _apiClient.GetDroplet(dropletId);
            var apiDroplet2 = _mapper.Map<Droplet>(response2);
            var apiTags2 = _mapper.Map<List<Tag>>(response2.Tags);
            var apiReg2 = _mapper.Map<List<Region>>(response2.Size.Regions);

            await UpdateTags(userId, apiTags2);
            await UpdateRegions(userId, apiReg2);

            return await UpdateDroplet(userId, apiDroplet2, dbDroplet);
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

                await UpdateDroplet(userId, apiDroplet, dbDroplet);
            }

            // remove missing droplets
            var apiDroplets = _mapper.Map<List<Droplet>>(droplets);
            foreach (var item in storedDroplets)
                await RemoveDropletRecord(userId, apiDroplets, item);
        }

        private async Task<Droplet> UpdateDroplet(int userId, Droplet apiDroplet, Droplet dbDroplet)
        {
            using (var tran = _dbContext.Database.BeginTransaction())
            {
                apiDroplet.Size.Droplet = null;
                apiDroplet.Size.SizeRegions = null;
                apiDroplet.Size.Id = dbDroplet.Sizeid;
                apiDroplet.Size.UpdatedByUserId = userId;
                await apiDroplet.Size.Update<Size>(_dbContext);

                apiDroplet.Id = dbDroplet.Id;
                apiDroplet.DropletTags = null;
                apiDroplet.UpdatedByUserId = userId;
                await apiDroplet.Update<Droplet>(_dbContext);

                var sizeReg = dbDroplet.Size.SizeRegions.Select(t => new SizeRegion() { Size = apiDroplet.Size, Region = t.Region }).ToList();
                foreach (var sr in sizeReg)
                {
                    var ids = await _dbContext.SizeRegion.Where(g => g.Size.Droplet.Id == dbDroplet.Id && g.Region.Name == sr.Region.Name).Select(t => t.Id).ToListAsync();
                    if (ids.Any())
                    {
                        sr.Id = Enumerable.Max(ids);
                        sr.UpdatedByUserId = userId;
                        await sr.Update<SizeRegion>(_dbContext);
                    }
                }

                var dTags = dbDroplet.DropletTags.Select(t => new DropletTag() { Droplet = apiDroplet, Tag = t.Tag }).ToList();
                foreach (var dt in dTags)
                {
                    var ids = await _dbContext.DropletTag.Where(g => g.Droplet.DoUid == dt.Droplet.DoUid && g.Tag.Name == dt.Tag.Name).Select(t => t.Id).ToListAsync();
                    if (ids.Any())
                    {
                        dt.Id = Enumerable.Max(ids);
                        dt.UpdatedByUserId = userId;
                        await dt.Update<DropletTag>(_dbContext);
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
                    var removedDroplet = _mapper.Map<Droplet>(item);

                    removedDroplet.Size.Droplet = null;
                    removedDroplet.Size.SizeRegions = null;
                    removedDroplet.Size.Id = item.Sizeid;
                    removedDroplet.Size.UpdatedByUserId = userId;
                    await removedDroplet.Size.Delete<Size>(_dbContext);

                    removedDroplet.Id = item.Id;
                    removedDroplet.DropletTags = null;
                    removedDroplet.UpdatedByUserId = userId;
                    await removedDroplet.Delete<Droplet>(_dbContext);

                    var sizeReg = item.Size.SizeRegions.Select(t => new SizeRegion() { Size = removedDroplet.Size, Region = t.Region }).ToList();
                    foreach (var sr in sizeReg)
                    {
                        var ids = await _dbContext.SizeRegion.Where(g => g.Size.Slug == sr.Size.Slug && g.Region.Name == sr.Region.Name).Select(t => t.Id).ToListAsync();
                        if (ids.Any())
                        {
                            sr.Id = Enumerable.Max(ids);
                            sr.UpdatedByUserId = userId;
                            await sr.Delete<SizeRegion>(_dbContext);
                        }
                    }

                    var dTags = item.DropletTags.Select(t => new DropletTag() { Droplet = removedDroplet, Tag = t.Tag }).ToList();
                    foreach (var dt in dTags)
                    {
                        var ids = await _dbContext.DropletTag.Where(g => g.Droplet.DoUid == dt.Droplet.DoUid && g.Tag.Name == dt.Tag.Name).Select(t => t.Id).ToListAsync();
                        if (ids.Any())
                        {
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
