using System.Collections.Generic;
using System.Linq;

namespace Microting.DigitalOceanBase.Infrastructure.Data.Entities
{
    public class Droplet : BaseEntity
    {
        public Droplet()
        {
            DropletTags = new List<DropletTag>();
        }

        public long DoUid { get; set; }
        public int CustomerNo { get; set; }//// leave it empty
        public string PublicIpV4 { get; set; }
        public string PrivateIpV4 { get; set; }
        public string PublicIpV6 { get; set; }
        public string CurrentImageName { get; set; }
        public string RequestedImageName { get; set; }// should be filled during rebuild
        public long CurrentImageId { get; set; }
        public long RequestedImageId { get; set; }//// should be filled during rebuild
        public string UserData { get; set; } // leave it empty
        public bool MonitoringEnabled { get; set; }
        public bool IpV6Enabled { get; set; }
        public bool BackupsEnabled { get; set; }
        public List<DropletTag> DropletTags { get; set; }
        public string Name { get; set; }

        public int Sizeid { get; set; }

        public bool Locked { get; set; }

        public string Status { get; set; }

        public Droplet(DigitalOcean.API.Models.Responses.Droplet apiDroplet)
        {
            DoUid = apiDroplet.Id;
            Name = apiDroplet.Name;
            PublicIpV4 = apiDroplet.Networks.V4.FirstOrDefault(g => g.Type == "public") == null
                ? null : apiDroplet.Networks.V4.FirstOrDefault(g => g.Type == "public").IpAddress;
            PrivateIpV4 = apiDroplet.Networks.V4.FirstOrDefault(g => g.Type != "public") == null
                ? null : apiDroplet.Networks.V4.FirstOrDefault(g => g.Type != "public").IpAddress;
            PublicIpV6 = apiDroplet.Networks.V6.FirstOrDefault(g => g.Type == "public") == null
                ? null : apiDroplet.Networks.V6.FirstOrDefault(g => g.Type == "public").IpAddress;
            CurrentImageName = apiDroplet.Image.Name;
            CurrentImageId = apiDroplet.Image.Id;
            if (apiDroplet.Features != null)
            {
                MonitoringEnabled = apiDroplet.Features.Any(t => t == "monitoring");
                IpV6Enabled = apiDroplet.Features.Any(t => t == "ipv6");
                BackupsEnabled = apiDroplet.Features.Any(t => t == "backups");
            }
            Locked = apiDroplet.Locked;
            Status = apiDroplet.Status;
        }
    }
}
