using System.ComponentModel.DataAnnotations.Schema;

namespace Microting.DigitalOceanBase.Infrastructure.Data.Entities
{
    public class SizeRegionVersion : BaseEntity
    {
        public int RegionId { get; set; }

        public int SizeId { get; set; }

        public int SizeRegionId { get; set; }
    }
}
