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
    public class FetchDropletTestsFixture : DbTestFixture
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
            var manager = new DigitalOceanManager(DbContext, apiClient.Object);

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
                CheckBaseCreateInfo(userId, tag);
                Assert.IsNotNull(tag.Name);
            }

            // regions
            Assert.IsTrue(createdRegions.Select(t => t.Name).Except(resp.Size.Regions).Count() == 0);
            foreach (var reg in createdRegions)
            {
                CheckBaseCreateInfo(userId, reg);
                Assert.IsNotNull(reg.Name);
            }

            // size
            CheckBaseCreateInfo(userId, createdSize);
            Assert.AreEqual(createdSize.Memory, resp.Size.Memory);
            Assert.AreEqual(createdSize.PriceHourly, resp.Size.PriceHourly);
            Assert.AreEqual(createdSize.PriceMonthly, resp.Size.PriceMonthly);
            Assert.AreEqual(createdSize.Slug, resp.Size.Slug);
            Assert.AreEqual(createdSize.Transfer, resp.Size.Transfer);
            Assert.AreEqual(createdSize.Vcpus, resp.Size.Vcpus);
            Assert.AreEqual(createdSize.Disk, resp.Size.Disk);

            // droplet
            CheckBaseCreateInfo(userId, createdDroplet);
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
                CheckBaseCreateInfo(userId, dt);

                Assert.AreEqual(createdDroplet.Id, dt.DropletId);
                Assert.IsTrue(createdTags.Select(t => t.Id).Contains(dt.Id));
            }

            // regionsize
            Assert.AreEqual(createdSizeRegions.Count, createdRegions.Count);
            foreach (var sr in createdSizeRegions)
            {
                CheckBaseCreateInfo(userId, sr);

                Assert.AreEqual(createdSize.Id, sr.SizeId);
                Assert.IsTrue(createdRegions.Select(t => t.Id).Contains(sr.Id));
            }
        }

        [Test]
        public async Task Droplet_UpdatedFromSyncSuccessfully()
        {
            // Arrange 
            var userId = 111;

            #region synced first droplet
            var firstDropletResponse = new DigitalOcean.API.Models.Responses.Droplet()
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

            var apiResp = Task.FromResult(new List<DigitalOcean.API.Models.Responses.Droplet>() { firstDropletResponse });

            var apiClient = new Mock<IApiClient>();
            apiClient.Setup(g => g.GetDropletsList()).Returns(apiResp);
            var manager = new DigitalOceanManager(DbContext, apiClient.Object);
            await manager.FetchDropletsAsync(userId);
            #endregion

            #region synced first droplet setup
            var secondDropletResponse = new DigitalOcean.API.Models.Responses.Droplet()
            {
                Id = 777,
                Size = new DigitalOcean.API.Models.Responses.Size()
                {
                    Regions = new List<string>() { "nyc3" },
                    Memory = 11,
                    PriceHourly = 55,
                    PriceMonthly = 305,
                    Slug = "khkhjkh5555",
                    Transfer = 2.65555,
                    Vcpus = 6555,
                    Disk = 22555
                },
                SizeSlug = "s-1vcpu-1gb55555",
                Name = "TestDroplet55555",
                Networks = new DigitalOcean.API.Models.Responses.Network
                {
                    V4 = new List<DigitalOcean.API.Models.Responses.InterfaceV4>
                    {
                        new DigitalOcean.API.Models.Responses.InterfaceV4()
                        {
                            IpAddress = "testv4555",
                            Type = "public",
                        },
                        new DigitalOcean.API.Models.Responses.InterfaceV4()
                        {
                            IpAddress = "testv4private5555",
                            Type = "private",
                        }
                    },
                    V6 = new List<DigitalOcean.API.Models.Responses.InterfaceV6>()
                    {
                        new DigitalOcean.API.Models.Responses.InterfaceV6()
                        {
                            IpAddress = "testv65555",
                            Type="public"
                        }
                    }
                },
                Tags = new List<string>() { "test", "Test2" },
                Image = new DigitalOcean.API.Models.Responses.Image
                {
                    Id = 8888888,
                    Name = "MyTestImage5555"
                },

                
               // Features = new List<string>() { "ipv6", "backups", "monitoring" }, intentionaly disable features
            };
            var apiResp2 = Task.FromResult(new List<DigitalOcean.API.Models.Responses.Droplet>() { secondDropletResponse });
            apiClient.Setup(g => g.GetDropletsList()).Returns(apiResp2);
            #endregion

            // Act
            await manager.FetchDropletsAsync(userId);


            // Assert
            var createdDroplet = (await DbContext.Droplets.ToListAsync()).Last();
            var createdSize = (await DbContext.Sizes.ToListAsync()).Last();
            var createdDropletTags = await DbContext.DropletTag.Where(t => t.DropletId == createdDroplet.Id).ToListAsync();
            var createdSizeRegions = await DbContext.SizeRegion.Where(t => t.SizeId == createdSize.Id).ToListAsync();
            var createdTags = (await DbContext.Tags.ToListAsync())
                .Where(t => createdDropletTags.Select(t => t.TagId)
                .Contains(t.Id))
                .ToList();
            var createdRegions = (await DbContext.Regions.ToListAsync())
                .Where(t => createdSizeRegions.Select(t => t.RegionId)
                .Contains(t.Id))
                .ToList();

            // tags
            Assert.IsTrue(createdTags.Select(t => t.Name).Except(secondDropletResponse.Tags).Count() == 0);
            foreach (var tag in createdTags)
            {
                CheckBaseCreateInfo(userId, tag);
                Assert.IsNotNull(tag.Name);
            }

            // regions
            Assert.IsTrue(createdRegions.Select(t => t.Name).Except(secondDropletResponse.Size.Regions).Count() == 0);
            foreach (var reg in createdRegions)
            {
                CheckBaseCreateInfo(userId, reg);
                Assert.IsNotNull(reg.Name);
            }

            // size
            CheckBaseUpdateInfo(userId, createdSize, 2);
            Assert.AreEqual(createdSize.Memory, secondDropletResponse.Size.Memory);
            Assert.AreEqual(createdSize.PriceHourly, secondDropletResponse.Size.PriceHourly);
            Assert.AreEqual(createdSize.PriceMonthly, secondDropletResponse.Size.PriceMonthly);
            Assert.AreEqual(createdSize.Slug, secondDropletResponse.Size.Slug);
            Assert.AreEqual(createdSize.Transfer, secondDropletResponse.Size.Transfer);
            Assert.AreEqual(createdSize.Vcpus, secondDropletResponse.Size.Vcpus);
            Assert.AreEqual(createdSize.Disk, secondDropletResponse.Size.Disk);

            // droplet
            CheckBaseUpdateInfo(userId, createdDroplet, 2);
            Assert.AreEqual(createdDroplet.DoUid, secondDropletResponse.Id);
            Assert.AreEqual(createdDroplet.CustomerNo, 0);
            Assert.AreEqual(createdDroplet.PublicIpV4, secondDropletResponse.Networks.V4[0].IpAddress);
            Assert.AreEqual(createdDroplet.PrivateIpV4, secondDropletResponse.Networks.V4[1].IpAddress);
            Assert.AreEqual(createdDroplet.PublicIpV6, secondDropletResponse.Networks.V6[0].IpAddress);
            Assert.AreEqual(createdDroplet.CurrentImageName, secondDropletResponse.Image.Name);
            Assert.IsNull(createdDroplet.RequestedImageName);
            Assert.AreEqual(createdDroplet.CurrentImageId, secondDropletResponse.Image.Id);
            Assert.AreEqual(createdDroplet.RequestedImageId, 0);
            Assert.IsNull(createdDroplet.UserData);
            Assert.AreEqual(createdDroplet.MonitoringEnabled, secondDropletResponse.Features?.Contains("monitoring") ?? false);
            Assert.AreEqual(createdDroplet.IpV6Enabled, secondDropletResponse.Features?.Contains("ipv6") ?? false);
            Assert.AreEqual(createdDroplet.BackupsEnabled, secondDropletResponse.Features?.Contains("backups") ?? false);
            Assert.AreEqual(createdDroplet.Sizeid, createdSize.Id);

            // dropletTag
            Assert.Greater(createdDropletTags.Count, 0);
            Assert.AreEqual(createdDropletTags.Count, createdTags.Count);
            foreach (var dt in createdDropletTags)
            {
                CheckBaseUpdateInfo(userId, dt);

                Assert.AreEqual(createdDroplet.Id, dt.DropletId);
                Assert.IsTrue(createdTags.Select(t => t.Id).Contains(dt.TagId));
            }

            // regionsize
            Assert.Greater(createdSizeRegions.Count, 0);
            Assert.AreEqual(createdSizeRegions.Count, createdRegions.Count);
            foreach (var sr in createdSizeRegions)
            {
                CheckBaseUpdateInfo(userId, sr);

                Assert.AreEqual(createdSize.Id, sr.SizeId);
                Assert.IsTrue(createdRegions.Select(t => t.Id).Contains(sr.RegionId));
            }
        }

        [Test]
        public async Task Droplet_RemovedFromSyncSuccessfully()
        {
            // Arrange 
            var userId = 111;

            #region synced first droplet
            var firstDropletResponse = new DigitalOcean.API.Models.Responses.Droplet()
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

            var apiResp = Task.FromResult(new List<DigitalOcean.API.Models.Responses.Droplet>() { firstDropletResponse });

            var apiClient = new Mock<IApiClient>();
            apiClient.Setup(g => g.GetDropletsList()).Returns(apiResp);
            var manager = new DigitalOceanManager(DbContext, apiClient.Object);
            await manager.FetchDropletsAsync(userId);
            #endregion


            var apiResp2 = Task.FromResult(new List<DigitalOcean.API.Models.Responses.Droplet>() { });
            apiClient.Setup(g => g.GetDropletsList()).Returns(apiResp2);

            // Act
            await manager.FetchDropletsAsync(userId);


            // Assert
            var createdDroplet = (await DbContext.Droplets.AsNoTracking().ToListAsync()).Last();
            var createdSize = (await DbContext.Sizes.AsNoTracking().ToListAsync()).Last();
            var createdDropletTags = await DbContext.DropletTag.AsNoTracking().Where(t => t.DropletId == createdDroplet.Id).ToListAsync();
            var createdSizeRegions = await DbContext.SizeRegion.AsNoTracking().Where(t => t.SizeId == createdSize.Id).ToListAsync();
            var createdTags = (await DbContext.Tags.AsNoTracking().ToListAsync())
                .Where(t => createdDropletTags.Select(t => t.TagId)
                .Contains(t.Id))
                .ToList();
            var createdRegions = (await DbContext.Regions.AsNoTracking().ToListAsync())
                .Where(t => createdSizeRegions.Select(t => t.RegionId)
                .Contains(t.Id))
                .ToList();

            // tags
            Assert.IsTrue(createdTags.Select(t => t.Name).Except(firstDropletResponse.Tags).Count() == 0);
            foreach (var tag in createdTags)
            {
                CheckBaseCreateInfo(userId, tag);
                Assert.IsNotNull(tag.Name);
            }

            // regions
            Assert.IsTrue(createdRegions.Select(t => t.Name).Except(firstDropletResponse.Size.Regions).Count() == 0);
            foreach (var reg in createdRegions)
            {
                CheckBaseCreateInfo(userId, reg);
                Assert.IsNotNull(reg.Name);
            }

            // size
            CheckBaseRemoveInfo(userId, createdSize);
            Assert.AreEqual(createdSize.Memory, firstDropletResponse.Size.Memory);
            Assert.AreEqual(createdSize.PriceHourly, firstDropletResponse.Size.PriceHourly);
            Assert.AreEqual(createdSize.PriceMonthly, firstDropletResponse.Size.PriceMonthly);
            Assert.AreEqual(createdSize.Slug, firstDropletResponse.Size.Slug);
            Assert.AreEqual(createdSize.Transfer, firstDropletResponse.Size.Transfer);
            Assert.AreEqual(createdSize.Vcpus, firstDropletResponse.Size.Vcpus);
            Assert.AreEqual(createdSize.Disk, firstDropletResponse.Size.Disk);

            // droplet
            CheckBaseRemoveInfo(userId, createdDroplet);
            Assert.AreEqual(createdDroplet.DoUid, firstDropletResponse.Id);
            Assert.AreEqual(createdDroplet.CustomerNo, 0);
            Assert.AreEqual(createdDroplet.PublicIpV4, firstDropletResponse.Networks.V4[0].IpAddress);
            Assert.AreEqual(createdDroplet.PrivateIpV4, firstDropletResponse.Networks.V4[1].IpAddress);
            Assert.AreEqual(createdDroplet.PublicIpV6, firstDropletResponse.Networks.V6[0].IpAddress);
            Assert.AreEqual(createdDroplet.CurrentImageName, firstDropletResponse.Image.Name);
            Assert.IsNull(createdDroplet.RequestedImageName);
            Assert.AreEqual(createdDroplet.CurrentImageId, firstDropletResponse.Image.Id);
            Assert.AreEqual(createdDroplet.RequestedImageId, 0);
            Assert.IsNull(createdDroplet.UserData);
            Assert.AreEqual(createdDroplet.MonitoringEnabled, firstDropletResponse.Features.Contains("monitoring"));
            Assert.AreEqual(createdDroplet.IpV6Enabled, firstDropletResponse.Features.Contains("ipv6"));
            Assert.AreEqual(createdDroplet.BackupsEnabled, firstDropletResponse.Features.Contains("backups"));
            Assert.AreEqual(createdDroplet.Sizeid, createdSize.Id);

            // dropletTag
            Assert.AreEqual(createdDropletTags.Count, createdTags.Count);
            foreach (var dt in createdDropletTags)
            {
                CheckBaseRemoveInfo(userId, dt);

                Assert.AreEqual(createdDroplet.Id, dt.DropletId);
                Assert.IsTrue(createdTags.Select(t => t.Id).Contains(dt.TagId));
            }

            // regionsize
            Assert.AreEqual(createdSizeRegions.Count, createdRegions.Count);
            foreach (var sr in createdSizeRegions)
            {
                CheckBaseCreateInfo(userId, sr);

                Assert.AreEqual(createdSize.Id, sr.SizeId);
                Assert.IsTrue(createdRegions.Select(t => t.Id).Contains(sr.RegionId));
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
            var manager = new DigitalOceanManager(DbContext, apiClient.Object);

            // Act
            await manager.FetchDropletsAsync(userId);


            // Assert
            var createdImage = await DbContext.Images.FirstOrDefaultAsync();

            CheckBaseCreateInfo(userId, createdImage);
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
                CreatedAt = DateTime.UtcNow,
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
            var manager = new DigitalOceanManager(DbContext, apiClient.Object);

            var firstImage = new Image() 
            {
                DoUid = 9999,
                Slug = "s-1vcpu-1gbimage",
                Name = "TestDroplet",
                Type = "test image type",
                Distribution = "test distribution",
                Public = true,
                CreatedAt = resp.CreatedAt,
                MinDiskSize = 50,
                SizeGigabytes = 53,
                Description = "test image description",
                Status = "running",
                CreatedByUserId = userId,
                Version = 1,
                WorkflowState = WorkflowStates.Created,
                ImageCreatedAt = resp.CreatedAt,
                ErrorMessage = null
            };
            await DbContext.Images.AddAsync(firstImage);
            await DbContext.SaveChangesAsync();

            // Act
            await manager.FetchDropletsAsync(userId);


            // Assert
            var updatedImageList = await DbContext.Images.ToListAsync();
            var updatedImage = updatedImageList.Last();

            CheckBaseUpdateInfo(userId, updatedImage, 2);
            Assert.AreEqual(updatedImage.DoUid, resp.Id);
            Assert.AreEqual(updatedImage.Name, resp.Name);
            Assert.AreEqual("test image typeupdate", resp.Type);
            Assert.AreEqual("test distributionupdate", resp.Distribution);
            Assert.AreEqual("s-1vcpu-1gbimageupdate", resp.Slug);
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
            var manager = new DigitalOceanManager(DbContext, apiClient.Object);

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

            CheckBaseRemoveInfo(userId, updatedImage);
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
