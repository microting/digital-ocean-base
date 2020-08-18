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

        public DigitalOceanManager(DigitalOceanDbContext dbContext, IApiClient apiClient)
        {
            _dbContext = dbContext;
            _apiClient = apiClient;
        }

        public async Task<Droplet> CreateDropletAsync(int userId, CreateDropletRequest request)
        {
            // check key name, or all keys
            var result = await _apiClient.CreateDroplet(request);

            var droplet = new Droplet(result);
            var size = new Size(result.Size);

            var regions = new List<Region>();
            foreach (string region in result.Size.Regions)
            {
                regions.Add(new Region() { Name = region});
            }
            var tags = new List<Tag>();
            foreach (string tag in result.Tags)
            {
                tags.Add(new Tag() { Name = tag});
            }

            await UpdateTags(userId, tags);
            await UpdateRegions(userId, regions);

            return await CreateDropletRecord(userId, droplet, size, tags, regions);
        }

        public async Task<Droplet> RebuildDropletAsync(int userId, int dropletId, int imageId)
        {
            // proper droplet resolving
            var droplet = await _dbContext.Droplets
                .FirstOrDefaultAsync(t => t.DoUid == dropletId);
            if (droplet == null)
                throw new NullReferenceException("Droplet is not found");

            var image = await _dbContext.Images.FirstOrDefaultAsync(t => t.DoUid == imageId);
            if (image == null)
                throw new NullReferenceException("Image is not found");

            var tags = await _dbContext.Tags
                .Where(t => droplet.DropletTags.Select(m => m.TagId).Contains(t.Id))
                .ToListAsync();

            // requested image

            var response = await _apiClient.GetDroplet(dropletId);
            var apiDroplet = new Droplet(response);
            var apiSize = new Size(response.Size);

            var apiReg = new List<Region>();
            foreach (string region in response.Size.Regions)
            {
                apiReg.Add(new Region() { Name = region});
            }
            var apiTags = new List<Tag>();
            foreach (string tag in response.Tags)
            {
                apiTags.Add(new Tag() { Name = tag});
            }
            // var apiDroplet = _mapper.Map<Droplet>(response);
            // var apiSize = _mapper.Map<Size>(response.Size);
            // var apiTags = _mapper.Map<List<Tag>>(response.Tags);
            // var apiReg = _mapper.Map<List<Region>>(response.Size.Regions);
            //
            apiDroplet.RequestedImageId = imageId;
            apiDroplet.RequestedImageName = image.Name;
            await UpdateDroplet(userId, apiDroplet, droplet, apiSize);
            //
            //
            var result = await _apiClient.RebuildDroplet(dropletId, imageId);
            while (result.Status != "completed")
            {
                Thread.Sleep(5000);
                result = await _apiClient.GetStatus(result.Id);

                if (result.Status == "errored")
                    throw new InvalidOperationException("Failed to rebuild droplet");
            }

            // created image
            var dbDroplets = await _dbContext.Droplets.Where(t => t.DoUid == dropletId).AsNoTracking().ToListAsync();
            var dbDroplet = dbDroplets.OrderBy(t => t.Id).LastOrDefault();

            var response2 = await _apiClient.GetDroplet(dropletId);

            var apiDroplet2 = new Droplet(response);
            var apiSize2 = new Size(response.Size);

            var apiReg2 = new List<Region>();
            foreach (string region in response.Size.Regions)
            {
                apiReg.Add(new Region() { Name = region});
            }

            var apiTags2 = new List<Tag>();
            foreach (string tag in response.Tags)
            {
                apiTags.Add(new Tag() { Name = tag});
            }
            await UpdateTags(userId, apiTags2);
            await UpdateRegions(userId, apiReg2);

            return await UpdateDroplet(userId, apiDroplet2, dbDroplet, apiSize2);
        }

        public async Task FetchDropletsAsync(int userId)
        {
            await SyncImageInfo(userId);
            await SyncDropletsInfo(userId);
        }


        private async Task SyncImageInfo(int userId)
        {
            var result = await _apiClient.GetImagesList();
            var images = new List<Image>();
            if (result != null)
            {
                foreach (var image in result)
                {
                    images.Add(new Image(image));
                }
            }

            var dbImages = await _dbContext.Images
                .Where(t => t.WorkflowState != WorkflowStates.Removed)
                .ToListAsync(); // check correct data loading

            var storedImages = dbImages
                .GroupBy(g => g.DoUid)
                .Select(m => m.OrderByDescending(h => h.Version).First());

            foreach (var item in images)
            {
                // add if was removed
                var dbImage = storedImages.LastOrDefault(t => t.DoUid == item.DoUid);
                if (dbImage == null)
                {
                    item.CreatedByUserId = userId;
                    item.UpdatedByUserId = userId;
                    await item.Create(_dbContext);
                    continue;
                }

                // check update
                dbImage.Description = item.Description;
                dbImage.Name = item.Name;
                dbImage.Public = item.Public;
                dbImage.Status = item.Status;
                dbImage.UpdatedByUserId = userId;
                await dbImage.Update(_dbContext);
            }

            foreach (var item in storedImages)
            {
                var image = images.LastOrDefault(t => t.DoUid == item.DoUid);
                if (image == null)
                {
                    item.UpdatedByUserId = userId;
                    await item.Delete(_dbContext);
                }
            }
        }

        private async Task SyncDropletsInfo(int userId)
        {
            var droplets = await _apiClient.GetDropletsList();
            var apiDroplets = new List<Droplet>();

            var dbDroplets = await _dbContext.Droplets
                .Include(t => t.DropletTags)
                .Where(t => t.WorkflowState != WorkflowStates.Removed)
                .ToListAsync();

            var storedDroplets = dbDroplets
                .GroupBy(g => g.DoUid)
                .Select(m => m.OrderByDescending(h => h.Version).First());

            foreach (var d in droplets)
            {
                var droplet = new Droplet(d);
                apiDroplets.Add(droplet);
                var size = new Size(d.Size);

                var regions = new List<Region>();
                foreach (string region in d.Size.Regions)
                {
                    regions.Add(new Region() { Name = region});
                }
                var tags = new List<Tag>();
                foreach (string tag in d.Tags)
                {
                    tags.Add(new Tag() { Name = tag});
                }

                await UpdateTags(userId, tags);
                await UpdateRegions(userId, regions);

                var dbDroplet = storedDroplets.FirstOrDefault(t => t.DoUid == droplet.DoUid);

                // add new droplet
                if (dbDroplet == null)
                {
                     await CreateDropletRecord(userId, droplet, size, tags, regions);
                     continue;
                }

                await UpdateDroplet(userId, droplet, dbDroplet, size);
            }

            // remove missing droplets
            foreach (var item in storedDroplets)
                await RemoveDropletRecord(userId, apiDroplets, item);
        }

        private async Task<Droplet> UpdateDroplet(int userId, Droplet apiDroplet, Droplet dbDroplet, Size size)
        {
            Droplet thisDroplet = await _dbContext.Droplets.SingleAsync(x => x.Id == dbDroplet.Id);
            thisDroplet.CustomerNo = apiDroplet.CustomerNo;
            thisDroplet.PublicIpV4 = apiDroplet.PublicIpV4;
            thisDroplet.PrivateIpV4 = apiDroplet.PrivateIpV4;
            thisDroplet.PublicIpV6 = apiDroplet.PublicIpV6;
            thisDroplet.Name = apiDroplet.Name;
            thisDroplet.CurrentImageName = apiDroplet.CurrentImageName;
            thisDroplet.CurrentImageId = apiDroplet.CurrentImageId;
            thisDroplet.MonitoringEnabled = apiDroplet.MonitoringEnabled;
            thisDroplet.IpV6Enabled = apiDroplet.IpV6Enabled;
            thisDroplet.BackupsEnabled = apiDroplet.BackupsEnabled;
            thisDroplet.RequestedImageName = apiDroplet.RequestedImageName;
            thisDroplet.RequestedImageId = apiDroplet.RequestedImageId;
            thisDroplet.UpdatedByUserId = userId;
            await thisDroplet.Update(_dbContext);

            Size dbSize = await _dbContext.Sizes.SingleOrDefaultAsync(x => x.Id == thisDroplet.Sizeid);
            if (dbSize != null)
            {
                dbSize.Slug = size.Slug;
                dbSize.Transfer = size.Transfer;
                dbSize.Disk = size.Disk;
                dbSize.Vcpus = size.Vcpus;
                dbSize.Memory = size.Memory;
                dbSize.PriceHourly = size.PriceHourly;
                dbSize.PriceMonthly = size.PriceMonthly;
                dbSize.UpdatedByUserId = userId;
                dbSize.DropletId = thisDroplet.Id;
                await dbSize.Update(_dbContext);
            }

            return dbDroplet;
        }

        private async Task RemoveDropletRecord(int userId, List<Droplet> apiDroplets, Droplet item)
        {
            var droplet = apiDroplets.FirstOrDefault(t => t.DoUid == item.DoUid);
            if (droplet == null)
            {
                var removedDroplet = item;
                Size size = await _dbContext.Sizes.SingleOrDefaultAsync(x => x.Id == removedDroplet.Sizeid);
                if (size != null)
                {
                    await size.Delete(_dbContext);
                }

                droplet = await _dbContext.Droplets.SingleAsync(x => x.Id == item.Id);
                droplet.UpdatedByUserId = userId;
                await droplet.Delete(_dbContext);

                var dTags = item.DropletTags.Select(t => new DropletTag() { Droplet = removedDroplet, Tag = t.Tag }).ToList();
                foreach (var dt in dTags)
                {
                    var dbDt = await _dbContext.DropletTag.SingleOrDefaultAsync(g => g.Droplet.DoUid == dt.Droplet.DoUid && g.Tag.Name == dt.Tag.Name);
                    if (dbDt != null)
                    {
                        //dt.Id = Enumerable.Max(ids);
                        dbDt.UpdatedByUserId = userId;
                        await dbDt.Delete(_dbContext);
                    }
                }
            }
        }

        private async Task<Droplet> CreateDropletRecord(int userId, Droplet droplet, Size size, List<Tag> tags, List<Region> regions)
        {
            if (!_dbContext.Sizes.Any(x => x.Slug == size.Slug))
            {
                size.CreatedByUserId = userId;
                size.UpdatedByUserId = userId;
                await size.Create(_dbContext);
            }
            else
            {
                size = _dbContext.Sizes.First(x => x.Slug == size.Slug);
            }

            foreach (var region in regions)
            {
                int reginonId = 0;
                if (!_dbContext.Regions.Any(x => x.Name == region.Name))
                {
                    await region.Create(_dbContext);
                    reginonId = region.Id;
                }
                else
                {
                    reginonId = _dbContext.Regions.First(x => x.Name == region.Name).Id;
                }
                var sr = new SizeRegion() { SizeId = size.Id, RegionId = reginonId, CreatedByUserId = userId, UpdatedByUserId = userId};
                await sr.Create(_dbContext);
            }

            droplet.Sizeid = size.Id;
            droplet.CreatedByUserId = userId;
            droplet.UpdatedByUserId = userId;
            await droplet.Create(_dbContext);

            foreach (var tag in tags)
            {
                var dt = new DropletTag() { DropletId = droplet.Id, TagId = tag.Id, CreatedByUserId = userId, UpdatedByUserId = userId };
                await dt.Create(_dbContext);
            }

            return droplet;
        }

        private async Task UpdateRegions(int userId, List<Region> regions)
        {
            for (int i = 0; i < regions.Count; i++)
            {
                var dbRegion = _dbContext.Regions.FirstOrDefault(t => t.Name == regions[i].Name && t.WorkflowState != WorkflowStates.Removed);
                if (dbRegion == null)
                {
                    regions[i].CreatedByUserId = userId;
                    regions[i].UpdatedByUserId = userId;
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
                    apiTags[i].UpdatedByUserId = userId;
                    await apiTags[i].Create(_dbContext);
                }
                else
                    apiTags[i] = dbTag;
            }
        }
    }
}
