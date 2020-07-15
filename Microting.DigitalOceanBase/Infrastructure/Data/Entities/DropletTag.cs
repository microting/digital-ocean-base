using System.ComponentModel.DataAnnotations.Schema;

namespace Microting.DigitalOceanBase.Infrastructure.Data.Entities
{
    public class DropletTag : BaseEntity
    {
        [ForeignKey("Id")]
        public int DropletId { get; set; }

        [ForeignKey("Id")]
        public int TagId { get; set; }

        public Droplet Droplet { get; set; }
        public Tag Tag { get; set; }
    }
}
