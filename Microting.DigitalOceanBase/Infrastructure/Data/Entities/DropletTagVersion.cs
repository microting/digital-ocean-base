using System.ComponentModel.DataAnnotations.Schema;

namespace Microting.DigitalOceanBase.Infrastructure.Data.Entities
{
    public class DropletTagVersion : BaseEntity
    {
        public int DropletId { get; set; }

        public int TagId { get; set; }
    }
}
