﻿using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Microting.DigitalOceanBase.Infrastructure.Data.Entities
{
    public class DropletVersion : BaseEntity
    {
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
        public string Name { get; set; }
        public int DropletId { get; set; }
        
        [ForeignKey("Id")]
        public int DropletTagId { get; set; }

        [ForeignKey("Id")]
        public int Sizeid { get; set; }

        public bool Locked { get; set; }

        public string Status { get; set; }
    }
}
