using DigitalOcean.API.Models.Responses;
using Microting.DigitalOceanBase.Infrastructure.Api.Clients.Requests;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microting.DigitalOceanBase.Infrastructure.Api.Clients
{
    public class MockApiClient : IApiClient
    {

        private Droplet dropletResponse = new Droplet()
        {
            Id = 777,
            Size = new Size()
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
            Networks = new Network
            {
                V4 = new List<InterfaceV4>
                    {
                        new InterfaceV4()
                        {
                            IpAddress = "testv4",
                            Type = "public",
                        },
                        new InterfaceV4()
                        {
                            IpAddress = "testv4private",
                            Type = "private",
                        }
                    },
                V6 = new List<InterfaceV6>()
                    {
                        new InterfaceV6()
                        {
                            IpAddress = "testv6",
                            Type="public"
                        }
                    }
            },
            Tags = new List<string>() { "test", "Test2" },
            Image = new Image
            {
                Id = 888,
                Name = "MyTestImage"
            },
            Features = new List<string>() { "ipv6", "backups", "monitoring" },
        };

        private Image imageResponse = new Image()
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

        private Droplet DropletResponse {
            get {
                dropletResponse.Id = new Random(Environment.TickCount).Next();
                return dropletResponse;
            }  
        }

        private Image ImageResponse
        {
            get
            {
                imageResponse.Id = new Random(Environment.TickCount).Next();
                return imageResponse;
            }
        }


        public Task<Droplet> CreateDroplet(CreateDropletRequest request) => Task.FromResult(DropletResponse);

        public Task<Droplet> GetDroplet(int dropletId) => Task.FromResult(DropletResponse);

        public Task<List<Droplet>> GetDropletsList() => Task.FromResult(new List<Droplet>() { DropletResponse });

        public Task<List<Image>> GetImagesList() => Task.FromResult(new List<Image>() { ImageResponse });

        public Task<DigitalOcean.API.Models.Responses.Action> GetStatus(long actionId) => Task.FromResult(new DigitalOcean.API.Models.Responses.Action() { Status = "completed" });

        public Task<DigitalOcean.API.Models.Responses.Action> RebuildDroplet(long dropletId, long imageId) => Task.FromResult(new DigitalOcean.API.Models.Responses.Action() { Status = "completed" });
    }
}
