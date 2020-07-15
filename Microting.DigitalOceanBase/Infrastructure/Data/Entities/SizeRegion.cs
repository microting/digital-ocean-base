using System.ComponentModel.DataAnnotations.Schema;

namespace Microting.DigitalOceanBase.Infrastructure.Data.Entities
{
    public class SizeRegion : BaseEntity
    {
        [ForeignKey("Id")]
        public int RegionId { get; set; }

        [ForeignKey("Id")]
        public int SizeId { get; set; }

        public Size Size { get; set; }
        public Region Region { get; set; }
    }
}
