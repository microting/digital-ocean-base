using Microsoft.EntityFrameworkCore;
using Microting.DigitalOceanBase.Infrastructure.Api.Clients;
using Microting.DigitalOceanBase.Infrastructure.Api.Clients.Requests;
using Microting.DigitalOceanBase.Managers;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microting.DigitalOceanBase.UnitTests
{
    public class CreateDropletTest : BaseTest
    {
        [Test]
        public async Task Droplet_CretedSuccessfully() 
        {
            // Arrange 
            var userId = 111;
            var apiResp = Task.FromResult(new DigitalOcean.API.Models.Responses.Droplet()
            {
                Id = 777,
                Size = new DigitalOcean.API.Models.Responses.Size()
                {
                    Regions = new List<string>() { "nyc3" },
                    Memory = 1,
                    PriceHourly=5,
                    PriceMonthly=30, 
                    Slug="khkhjkh",
                    Transfer = 2.6,
                    Vcpus = 6,
                    Disk= 22
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
            }) ;


            var apiClient = new Mock<IApiClient>();
            apiClient.Setup(g => g.CreateDroplet(It.IsAny<CreateDropletRequest>())).Returns(apiResp);
            var manager = new DigitalOceanManager(DbContext, apiClient.Object, Mapper);

            // Act
            var droplet = await manager.CreateDropletAsync(userId, new CreateDropletRequest()
            {
                Name = "TestDroplet",
                Region = "nyc3",
                Size = "s-1vcpu-1gb",
                Image = "ubuntu-16-04-x64",
                Tags = new List<string>() { "test", "Test2" },
                Ipv6 = true,
                PrivateNetworking = true,
                Monitoring = true,
            });

            // Assert
            var createdDroplet = await DbContext.Droplets.FirstOrDefaultAsync();
            var createdSize = await DbContext.Sizes.FirstOrDefaultAsync();
            var createdTags = await DbContext.Tags.ToListAsync();
            var createdDropletTags = await DbContext.DropletTag.ToListAsync();
            var createdRegions = await DbContext.Regions.ToListAsync();
            var createdSizeRegions = await DbContext.SizeRegion.ToListAsync();

            // tags
            Assert.IsTrue(createdTags.Select(t => t.Name).Except(apiResp.Result.Tags).Count() == 0 );
            foreach (var tag in createdTags)
            {
                CheckBaseCreateInfo(userId, tag);
                Assert.IsNotNull(tag.Name);
            }

            // regions
            Assert.IsTrue(createdRegions.Select(t => t.Name).Except(apiResp.Result.Size.Regions).Count() == 0);
            foreach (var reg in createdRegions)
            {
                CheckBaseCreateInfo(userId, reg);
                Assert.IsNotNull(reg.Name);
            }

            // size
            CheckBaseCreateInfo(userId, createdSize);
            Assert.AreEqual(createdSize.Memory, apiResp.Result.Size.Memory);
            Assert.AreEqual(createdSize.PriceHourly, apiResp.Result.Size.PriceHourly);
            Assert.AreEqual(createdSize.PriceMonthly, apiResp.Result.Size.PriceMonthly);
            Assert.AreEqual(createdSize.Slug, apiResp.Result.Size.Slug);
            Assert.AreEqual(createdSize.Transfer, apiResp.Result.Size.Transfer);
            Assert.AreEqual(createdSize.Vcpus, apiResp.Result.Size.Vcpus);
            Assert.AreEqual(createdSize.Disk, apiResp.Result.Size.Disk);

            // droplet
            CheckBaseCreateInfo(userId, createdDroplet);
            Assert.AreEqual(createdDroplet.DoUid, apiResp.Result.Id);
            Assert.AreEqual(createdDroplet.CustomerNo, 0);
            Assert.AreEqual(createdDroplet.PublicIpV4, apiResp.Result.Networks.V4[0].IpAddress);
            Assert.AreEqual(createdDroplet.PrivateIpV4, apiResp.Result.Networks.V4[1].IpAddress);
            Assert.AreEqual(createdDroplet.PublicIpV6, apiResp.Result.Networks.V6[0].IpAddress);
            Assert.AreEqual(createdDroplet.CurrentImageName, apiResp.Result.Image.Name);
            Assert.IsNull(createdDroplet.RequestedImageName);
            Assert.AreEqual(createdDroplet.CurrentImageId, apiResp.Result.Image.Id);
            Assert.AreEqual(createdDroplet.RequestedImageId, 0);
            Assert.IsNull(createdDroplet.UserData);
            Assert.AreEqual(createdDroplet.MonitoringEnabled, apiResp.Result.Features.Contains("monitoring"));
            Assert.AreEqual(createdDroplet.IpV6Enabled, apiResp.Result.Features.Contains("ipv6"));
            Assert.AreEqual(createdDroplet.BackupsEnabled, apiResp.Result.Features.Contains("backups"));
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

    }
}
