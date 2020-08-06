using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Microting.DigitalOceanBase.Infrastructure.Data.Entities
{
    public class Droplet : BaseEntity
    {
        public Droplet()
        {
            DropletTags = new List<DropletTag>();
        }

        public int DoUid { get; set; }
        public int CustomerNo { get; set; }//// leave it empty
        public string PublicIpV4 { get; set; }
        public string PrivateIpV4 { get; set; }
        public string PublicIpV6 { get; set; }
        public string CurrentImageName { get; set; }
        public string RequestedImageName { get; set; }// should be filled during rebuild
        public int CurrentImageId { get; set; }
        public int RequestedImageId { get; set; }//// should be filled during rebuild
        public string UserData { get; set; } // leave it empty
        public bool MonitoringEnabled { get; set; }
        public bool IpV6Enabled { get; set; }
        public bool BackupsEnabled { get; set; }
        public Size Size { get; set; }
        public List<DropletTag> DropletTags { get; set; }
        public string Name { get; set; }

        [ForeignKey("Id")]
        public int Sizeid { get; set; }
    }
}
