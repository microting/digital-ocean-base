using Microsoft.EntityFrameworkCore;
using Microting.DigitalOceanBase.Infrastructure.Api.Clients;
using Microting.DigitalOceanBase.Infrastructure.Constants;
using Microting.DigitalOceanBase.Infrastructure.Data.Entities;
using Microting.DigitalOceanBase.Managers;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microting.DigitalOceanBase.UnitTests
{
    public class FetchDropletTests : BaseTest
    {

        [Test]
        public async Task Droplet_CreatedFromSyncSuccessfully()
        {
            // Arrange 
            var userId = 111;
            var resp = new DigitalOcean.API.Models.Responses.Droplet()
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

            var apiResp = Task.FromResult(new List<DigitalOcean.API.Models.Responses.Droplet>() { resp });

            var apiClient = new Mock<IApiClient>();
            apiClient.Setup(g => g.GetDropletsList()).Returns(apiResp);
            var manager = new DigitalOceanManager(DbContext, apiClient.Object, Mapper);

            // Act
            await manager.FetchDropletsAsync(userId);


            // Assert
            var createdDroplet = await DbContext.Droplets.FirstOrDefaultAsync();
            var createdSize = await DbContext.Sizes.FirstOrDefaultAsync();
            var createdTags = await DbContext.Tags.ToListAsync();
            var createdDropletTags = await DbContext.DropletTag.ToListAsync();
            var createdRegions = await DbContext.Regions.ToListAsync();
            var createdSizeRegions = await DbContext.SizeRegion.ToListAsync();

            // tags
            Assert.IsTrue(createdTags.Select(t => t.Name).Except(resp.Tags).Count() == 0);
            foreach (var tag in createdTags)
            {
                ChekcBaseCreateInfo(userId, tag);
                Assert.IsNotNull(tag.Name);
            }

            // regions
            Assert.IsTrue(createdRegions.Select(t => t.Name).Except(resp.Size.Regions).Count() == 0);
            foreach (var reg in createdRegions)
            {
                ChekcBaseCreateInfo(userId, reg);
                Assert.IsNotNull(reg.Name);
            }

            // size
            ChekcBaseCreateInfo(userId, createdSize);
            Assert.AreEqual(createdSize.Memory, resp.Size.Memory);
            Assert.AreEqual(createdSize.PriceHourly, resp.Size.PriceHourly);
            Assert.AreEqual(createdSize.PriceMonthly, resp.Size.PriceMonthly);
            Assert.AreEqual(createdSize.Slug, resp.Size.Slug);
            Assert.AreEqual(createdSize.Transfer, resp.Size.Transfer);
            Assert.AreEqual(createdSize.Vcpus, resp.Size.Vcpus);
            Assert.AreEqual(createdSize.Disk, resp.Size.Disk);

            // droplet
            ChekcBaseCreateInfo(userId, createdDroplet);
            Assert.AreEqual(createdDroplet.DoUid, resp.Id);
            Assert.AreEqual(createdDroplet.CustomerNo, 0);
            Assert.AreEqual(createdDroplet.PublicIpV4, resp.Networks.V4[0].IpAddress);
            Assert.AreEqual(createdDroplet.PrivateIpV4, resp.Networks.V4[1].IpAddress);
            Assert.AreEqual(createdDroplet.PublicIpV6, resp.Networks.V6[0].IpAddress);
            Assert.AreEqual(createdDroplet.CurrentImageName, resp.Image.Name);
            Assert.IsNull(createdDroplet.RequestedImageName);
            Assert.AreEqual(createdDroplet.CurrentImageId, resp.Image.Id);
            Assert.AreEqual(createdDroplet.RequestedImageId, 0);
            Assert.IsNull(createdDroplet.UserData);
            Assert.AreEqual(createdDroplet.MonitoringEnabled, resp.Features.Contains("monitoring"));
            Assert.AreEqual(createdDroplet.IpV6Enabled, resp.Features.Contains("ipv6"));
            Assert.AreEqual(createdDroplet.BackupsEnabled, resp.Features.Contains("backups"));
            Assert.AreEqual(createdDroplet.Sizeid, createdSize.Id);

            // dropletTag
            Assert.AreEqual(createdDropletTags.Count, createdTags.Count);
            foreach (var dt in createdDropletTags)
            {
                ChekcBaseCreateInfo(userId, dt);

                Assert.AreEqual(createdDroplet.Id, dt.DropletId);
                Assert.IsTrue(createdTags.Select(t => t.Id).Contains(dt.Id));
            }

            // regionsize
            Assert.AreEqual(createdSizeRegions.Count, createdRegions.Count);
            foreach (var sr in createdSizeRegions)
            {
                ChekcBaseCreateInfo(userId, sr);

                Assert.AreEqual(createdSize.Id, sr.SizeId);
                Assert.IsTrue(createdRegions.Select(t => t.Id).Contains(sr.Id));
            }
        }

        [Test]
        public async Task Image_CreatedFromSyncSuccessfully()
        {
            // Arrange 
            var userId = 111;
            var resp = new DigitalOcean.API.Models.Responses.Image()
            {
                Id = 888,
                Slug = "s-1vcpu-1gbimage",
                Name = "TestDroplet",
                Tags = new List<string>() { "test", "Test2" },
                Type=  "test image type",
                Distribution = "test distribution",
                Public = true,
                Regions = new List<string>() { "nyc3" },
                CreatedAt = DateTime.Now,
                MinDiskSize = 50,
                SizeGigabytes = 53,
                Description = "test image description",
                Status ="running",
                ErrorMessage = null
            };

            var apiResp = Task.FromResult(new List<DigitalOcean.API.Models.Responses.Image>() { resp });

            var apiClient = new Mock<IApiClient>();
            apiClient.Setup(g => g.GetDropletsList()).Returns(Task.FromResult(new List<DigitalOcean.API.Models.Responses.Droplet>()));
            apiClient.Setup(g => g.GetImagesList()).Returns(apiResp);
            var manager = new DigitalOceanManager(DbContext, apiClient.Object, Mapper);

            // Act
            await manager.FetchDropletsAsync(userId);


            // Assert
            var createdImage = await DbContext.Images.FirstOrDefaultAsync();

            ChekcBaseCreateInfo(userId, createdImage);
            Assert.AreEqual(createdImage.DoUid, resp.Id);
            Assert.AreEqual(createdImage.Name, resp.Name);
            Assert.AreEqual(createdImage.Type, resp.Type);
            Assert.AreEqual(createdImage.Distribution, resp.Distribution);
            Assert.AreEqual(createdImage.Slug, resp.Slug);
            Assert.AreEqual(createdImage.Public, resp.Public);
            Assert.AreEqual(createdImage.ImageCreatedAt, resp.CreatedAt);
            Assert.AreEqual(createdImage.MinDiskSize, resp.MinDiskSize);
            Assert.AreEqual(createdImage.SizeGigabytes, resp.SizeGigabytes);
            Assert.AreEqual(createdImage.Description, resp.Description);
            Assert.AreEqual(createdImage.Status, resp.Status);
            Assert.IsNull(createdImage.ErrorMessage);
        }

        [Test]
        public async Task Image_UpdatedFromSyncSuccessfully()
        {
            // Arrange 
            var userId = 111;
            var resp = new DigitalOcean.API.Models.Responses.Image()
            {
                Id = 9999,
                Slug = "s-1vcpu-1gbimageupdate",
                Name = "TestDropletupdate",
                Tags = new List<string>() { "test", "Test2" },
                Type = "test image typeupdate",
                Distribution = "test distributionupdate",
                Public = true,
                Regions = new List<string>() { "nyc3" },
                CreatedAt = DateTime.Now,
                MinDiskSize = 50,
                SizeGigabytes = 53,
                Description = "test image description update",
                Status = "running",
                ErrorMessage = null
            };

            var apiResp = Task.FromResult(new List<DigitalOcean.API.Models.Responses.Image>() { resp });

            var apiClient = new Mock<IApiClient>();
            apiClient.Setup(g => g.GetDropletsList()).Returns(Task.FromResult(new List<DigitalOcean.API.Models.Responses.Droplet>()));
            apiClient.Setup(g => g.GetImagesList()).Returns(apiResp);
            var manager = new DigitalOceanManager(DbContext, apiClient.Object, Mapper);

            var firstImage = new Image() 
            {
                DoUid = 9999,
                Slug = "s-1vcpu-1gbimage",
                Name = "TestDroplet",
                Type = "test image type",
                Distribution = "test distribution",
                Public = true,
                CreatedAt = DateTime.Now,
                MinDiskSize = 50,
                SizeGigabytes = 53,
                Description = "test image description",
                Status = "running",
                CreatedByUserId = userId,
                Version = 1,
                WorkflowState = WorkflowStates.Created,
                ImageCreatedAt = DateTime.Now,
                ErrorMessage = null
            };
            await DbContext.Images.AddAsync(firstImage);
            await DbContext.SaveChangesAsync();

            // Act
            await manager.FetchDropletsAsync(userId);


            // Assert
            var updatedImageList = await DbContext.Images.ToListAsync();
            var updatedImage = updatedImageList.Last();

            ChekcBaseUpdateInfo(userId, updatedImage);
            Assert.AreEqual(updatedImage.DoUid, resp.Id);
            Assert.AreEqual(updatedImage.Name, resp.Name);
            Assert.AreEqual(updatedImage.Type, resp.Type);
            Assert.AreEqual(updatedImage.Distribution, resp.Distribution);
            Assert.AreEqual(updatedImage.Slug, resp.Slug);
            Assert.AreEqual(updatedImage.Public, resp.Public);
            Assert.AreEqual(updatedImage.ImageCreatedAt, resp.CreatedAt);
            Assert.AreEqual(updatedImage.MinDiskSize, resp.MinDiskSize);
            Assert.AreEqual(updatedImage.SizeGigabytes, resp.SizeGigabytes);
            Assert.AreEqual(updatedImage.Description, resp.Description);
            Assert.AreEqual(updatedImage.Status, resp.Status);
            Assert.IsNull(updatedImage.ErrorMessage);
        }

        [Test]
        public async Task Image_RemovedAfterSyncSuccessfully()
        {
            // Arrange 
            var userId = 111;
            var apiResp = Task.FromResult(new List<DigitalOcean.API.Models.Responses.Image>() { });

            var apiClient = new Mock<IApiClient>();
            apiClient.Setup(g => g.GetDropletsList()).Returns(Task.FromResult(new List<DigitalOcean.API.Models.Responses.Droplet>()));
            apiClient.Setup(g => g.GetImagesList()).Returns(apiResp);
            var manager = new DigitalOceanManager(DbContext, apiClient.Object, Mapper);

            var firstImage = new Image()
            {
                DoUid = 9999,
                Slug = "s-1vcpu-1gbimage",
                Name = "TestDroplet",
                Type = "test image type",
                Distribution = "test distribution",
                Public = true,
                CreatedAt = DateTime.Now,
                MinDiskSize = 50,
                SizeGigabytes = 53,
                Description = "test image description",
                Status = "running",
                CreatedByUserId = userId,
                Version = 1,
                WorkflowState = WorkflowStates.Created,
                ImageCreatedAt = DateTime.Now,
                ErrorMessage = null
            };
            await DbContext.Images.AddAsync(firstImage);
            await DbContext.SaveChangesAsync();

            // Act
            await manager.FetchDropletsAsync(userId);


            // Assert
            var updatedImageList = await DbContext.Images.ToListAsync();
            var updatedImage = updatedImageList.Last();

            ChekcBaseRemoveInfo(userId, updatedImage);
            Assert.AreEqual(updatedImage.DoUid, firstImage.DoUid);
            Assert.AreEqual(updatedImage.Name, firstImage.Name);
            Assert.AreEqual(updatedImage.Type, firstImage.Type);
            Assert.AreEqual(updatedImage.Distribution, firstImage.Distribution);
            Assert.AreEqual(updatedImage.Slug, firstImage.Slug);
            Assert.AreEqual(updatedImage.Public, firstImage.Public);
            Assert.AreEqual(updatedImage.ImageCreatedAt, firstImage.ImageCreatedAt);
            Assert.AreEqual(updatedImage.MinDiskSize, firstImage.MinDiskSize);
            Assert.AreEqual(updatedImage.SizeGigabytes, firstImage.SizeGigabytes);
            Assert.AreEqual(updatedImage.Description, firstImage.Description);
            Assert.AreEqual(updatedImage.Status, firstImage.Status);
            Assert.IsNull(updatedImage.ErrorMessage);
        }
    }
}
