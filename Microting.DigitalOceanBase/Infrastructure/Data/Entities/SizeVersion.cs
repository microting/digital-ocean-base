using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Microting.DigitalOceanBase.Infrastructure.Data.Entities
{
    public class SizeVersion : BaseEntity
    {
        public int DropletId { get; set; }
        public string Slug { get; set; }
        public double Transfer { get; set; }
        public decimal PriceMonthly { get; set; }
        public decimal PriceHourly { get; set; }
        public int Memory { get; set; }
        public int Vcpus { get; set; }
        public int Disk { get; set; }
        public bool Available { get; set; }
        public int SizeId { get; set; }
    }
}
