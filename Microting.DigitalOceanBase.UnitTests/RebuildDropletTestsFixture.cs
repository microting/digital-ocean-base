using Microsoft.EntityFrameworkCore;
using Microting.DigitalOceanBase.Infrastructure.Api.Clients;
using Microting.DigitalOceanBase.Managers;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microting.DigitalOceanBase.UnitTests
{
    public class RebuildDropletTestsFixture : DbTestFixture
    {

        [Test]
        public async Task DropletRebuildSuccessfull()
        {
            // Arrange 
            var userId = 111;
            var dropletResp = new DigitalOcean.API.Models.Responses.Droplet()
            {
                Id = 777,
                Size = new DigitalOcean.API.Models.Responses.Size()
                {
                    Regions = new List<string>() { "nyc3" },
                    Memory = 1,
                    PriceHourly = 5,
                    PriceMonthly = 30,
                    Slug = "khkhjkh",
                    Transfer = 2.6,
                    Vcpus = 6,
                    Disk = 22
                },
                SizeSlug = "s-1vcpu-1gb",
                Name = "TestDroplet",
                Networks = new DigitalOcean.API.Models.Responses.Network
                {
                    V4 = new List<DigitalOcean.API.Models.Responses.InterfaceV4>
                    {
                        new DigitalOcean.API.Models.Responses.InterfaceV4()
                        {
                            IpAddress = "testv4",
                            Type = "public",
                        },
                        new DigitalOcean.API.Models.Responses.InterfaceV4()
                        {
                            IpAddress = "testv4private",
                            Type = "private",
                        }
                    },
                    V6 = new List<DigitalOcean.API.Models.Responses.InterfaceV6>()
                    {
                        new DigitalOcean.API.Models.Responses.InterfaceV6()
                        {
                            IpAddress = "testv6",
                            Type="public"
                        }
                    }
                },
                Tags = new List<string>() { "test", "Test2" },
                Image = new DigitalOcean.API.Models.Responses.Image
                {
                    Id = 888,
                    Name = "MyTestImage"
                },
                Features = new List<string>() { "ipv6", "backups", "monitoring" },
            };
            var imageResp = new DigitalOcean.API.Models.Responses.Image()
            {
                Id = 888,
                Slug = "s-1vcpu-1gbimage",
                Name = "TestDroplet",
                Tags = new List<string>() { "test", "Test2" },
                Type = "test image type",
                Distribution = "test distribution",
                Public = true,
                Regions = new List<string>() { "nyc3" },
                CreatedAt = DateTime.Now,
                MinDiskSize = 50,
                SizeGigabytes = 53,
                Description = "test image description",
                Status = "running",
                ErrorMessage = null
            };

            var rebuildResp = new DigitalOcean.API.Models.Responses.Action() { Status = "completed" };
            var apiDropletResp = Task.FromResult(new List<DigitalOcean.API.Models.Responses.Droplet>() { dropletResp });
            var apiImageResp = Task.FromResult(new List<DigitalOcean.API.Models.Responses.Image>() { imageResp });

            var apiClient = new Mock<IApiClient>();
            apiClient.Setup(g => g.GetDroplet(It.IsAny<int>())).Returns(Task.FromResult(dropletResp));
            apiClient.Setup(g => g.GetDropletsList()).Returns(apiDropletResp);
            apiClient.Setup(g => g.RebuildDroplet(It.IsAny<long>(), It.IsAny<long>())).Returns(Task.FromResult(rebuildResp));
            apiClient.Setup(g => g.GetImagesList()).Returns(apiImageResp);
            var manager = new DigitalOceanManager(DbContext, apiClient.Object, Mapper);

            // Act
            await manager.FetchDropletsAsync(userId);
            await manager.RebuildDropletAsync(userId, 777, 888);

            // Assert
            var createdDroplet = await DbContext.Droplets.Where(t => t.Id == 1).FirstOrDefaultAsync();
            var createdSize = await DbContext.Sizes.Where(t => t.Id == 1).FirstOrDefaultAsync();
            var createdTags = await DbContext.Tags.ToListAsync();
            var createdDropletTags = await DbContext.DropletTag.Where(t => t.DropletId == createdDroplet.Id).ToListAsync();
            var createdRegions = await DbContext.Regions.ToListAsync();
            var createdSizeRegions = await DbContext.SizeRegion.Where(t => t.SizeId == createdSize.Id).ToListAsync();

            // tags
            Assert.IsTrue(createdTags.Select(t => t.Name).Except(dropletResp.Tags).Count() == 0);
            foreach (var tag in createdTags)
            {
                CheckBaseCreateInfo(userId, tag);
                Assert.IsNotNull(tag.Name);
            }

            // regions
            Assert.IsTrue(createdRegions.Select(t => t.Name).Except(dropletResp.Size.Regions).Count() == 0);
            foreach (var reg in createdRegions)
            {
                CheckBaseCreateInfo(userId, reg);
                Assert.IsNotNull(reg.Name);
            }

            // size
            CheckBaseUpdateInfo(userId, createdSize, 2);
            Assert.AreEqual(createdSize.Memory, dropletResp.Size.Memory);
            Assert.AreEqual(createdSize.PriceHourly, dropletResp.Size.PriceHourly);
            Assert.AreEqual(createdSize.PriceMonthly, dropletResp.Size.PriceMonthly);
            Assert.AreEqual(createdSize.Slug, dropletResp.Size.Slug);
            Assert.AreEqual(createdSize.Transfer, dropletResp.Size.Transfer);
            Assert.AreEqual(createdSize.Vcpus, dropletResp.Size.Vcpus);
            Assert.AreEqual(createdSize.Disk, dropletResp.Size.Disk);

            // droplet
            CheckBaseUpdateInfo(userId, createdDroplet, 3);
            Assert.AreEqual(createdDroplet.DoUid, dropletResp.Id);
            Assert.AreEqual(createdDroplet.CustomerNo, 0);
            Assert.AreEqual(createdDroplet.PublicIpV4, dropletResp.Networks.V4[0].IpAddress);
            Assert.AreEqual(createdDroplet.PrivateIpV4, dropletResp.Networks.V4[1].IpAddress);
            Assert.AreEqual(createdDroplet.PublicIpV6, dropletResp.Networks.V6[0].IpAddress);
            Assert.AreEqual(createdDroplet.CurrentImageName, dropletResp.Image.Name);
            Assert.AreEqual(null, createdDroplet.RequestedImageName);
            Assert.AreEqual(createdDroplet.CurrentImageId, dropletResp.Image.Id);
            Assert.AreEqual(0,createdDroplet.RequestedImageId);
            Assert.IsNull(createdDroplet.UserData);
            Assert.AreEqual(createdDroplet.MonitoringEnabled, dropletResp.Features.Contains("monitoring"));
            Assert.AreEqual(createdDroplet.IpV6Enabled, dropletResp.Features.Contains("ipv6"));
            Assert.AreEqual(createdDroplet.BackupsEnabled, dropletResp.Features.Contains("backups"));
            Assert.AreEqual(createdDroplet.Sizeid, createdSize.Id);

            // TODO Add tests for DropletVersions

            // dropletTag
            Assert.AreEqual(createdDropletTags.Count, createdTags.Count);
            foreach (var dt in createdDropletTags)
            {
                CheckBaseUpdateInfo(userId, dt);

                Assert.AreEqual(createdDroplet.Id, dt.DropletId);
                Assert.IsTrue(createdTags.Select(t => t.Id).Contains(dt.TagId));
            }

            // regionsize
            Assert.AreEqual(createdSizeRegions.Count, createdRegions.Count);
            foreach (var sr in createdSizeRegions)
            {
                CheckBaseUpdateInfo(userId, sr);

                Assert.AreEqual(createdSize.Id, sr.SizeId);
                Assert.IsTrue(createdRegions.Select(t => t.Id).Contains(sr.RegionId));
            }
        }
    }
}
