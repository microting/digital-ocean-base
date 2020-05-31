using Microsoft.EntityFrameworkCore;
using Microting.DigitalOceanBase.Infrastructure.Data.Entities;

namespace Microting.DigitalOceanBase.Infrastructure.Data
{
    public class DigitalOceanDbContext : DbContext
    {

        public DigitalOceanDbContext(DbContextOptions<DigitalOceanDbContext> options) : base(options)
        {

        }

        public DbSet<Droplet> Droplets { get; set; }
        public DbSet<Tag> Tags { get; set; }
        public DbSet<Image> Images { get; set; }
        public DbSet<DropletTag> DropletTag { get; set; }

        public DbSet<Credential> Credentials { get; set; }  // shoould have default methods and base entity?
    }
}
