using System.Collections.Generic;

namespace Microting.DigitalOceanBase.Infrastructure.Data.Entities
{
    public class Size: BaseEntity
    {
        public Size()
        {
            SizeRegions = new List<SizeRegion>();
        }

        public int DropletId { get; set; }
        public List<SizeRegion> SizeRegions { get; set; }
        public string Slug { get; set; }
        public double Transfer { get; set; }
        public decimal PriceMonthly { get; set; }
        public decimal PriceHourly { get; set; }
        public int Memory { get; set; }
        public int Vcpus { get; set; }
        public int Disk { get; set; }
        public bool Available { get; set; }
    }
}
