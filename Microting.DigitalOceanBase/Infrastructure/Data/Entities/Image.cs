using System;

namespace Microting.DigitalOceanBase.Infrastructure.Data.Entities
{
    public class Image : BaseEntity
    {
        public long DoUid { get; set; }
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

        public Image()
        {

        }

        public Image(DigitalOcean.API.Models.Responses.Image apiImage)
        {
            DoUid = apiImage.Id;
            Name = apiImage.Name;
            Type = apiImage.Type;
            Distribution = apiImage.Distribution;
            Slug = apiImage.Slug;
            Public = apiImage.Public;
            ImageCreatedAt = apiImage.CreatedAt;
            MinDiskSize = apiImage.MinDiskSize;
            SizeGigabytes = apiImage.SizeGigabytes;
            Description = apiImage.Description;
            Status = apiImage.Status;
            ErrorMessage = apiImage.ErrorMessage;
        }
    }
}
