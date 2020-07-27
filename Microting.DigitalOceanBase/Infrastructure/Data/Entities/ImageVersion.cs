using System;

namespace Microting.DigitalOceanBase.Infrastructure.Data.Entities
{
    public class ImageVersion : BaseEntity
    {
        public int DoUid { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public string Distribution { get; set; }
        public string Slug { get; set; }
        public bool Public { get; set; }
        //public List<Region> Regions { get; set; }
        public DateTime ImageCreatedAt { get; set; }
        public int MinDiskSize { get; set; }
        public double SizeGigabytes { get; set; }
        public string Description { get; set; }
        //public List<Tag> Tags { get; set; }
        public string Status { get; set; }
        public string ErrorMessage { get; set; }
    }
}
