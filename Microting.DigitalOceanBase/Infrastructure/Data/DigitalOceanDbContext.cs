using Microsoft.EntityFrameworkCore;
using Microting.DigitalOceanBase.Infrastructure.Data.Entities;
using Microting.eFormApi.BasePn.Abstractions;
using Microting.eFormApi.BasePn.Infrastructure.Database.Entities;

namespace Microting.DigitalOceanBase.Infrastructure.Data
{
    public class DigitalOceanDbContext : DbContext, IPluginDbContext
    {

        public DigitalOceanDbContext(DbContextOptions<DigitalOceanDbContext> options) : base(options)
        {

        }

        public DbSet<Droplet> Droplets { get; set; }
        public DbSet<Tag> Tags { get; set; }
        public DbSet<Image> Images { get; set; }
        public DbSet<DropletTag> DropletTag { get; set; }
        public DbSet<Size> Sizes { get; set; }
        public DbSet<Region> Regions { get; set; }
        public DbSet<SizeRegion> SizeRegion { get; set; }
        public DbSet<DropletVersion> DropletVersions { get; set; }
        public DbSet<ImageVersion> ImageVersions { get; set; }
        public DbSet<DropletTagVersion> DropletTagVersions { get; set; }
        public DbSet<SizeVersion> SizeVersions { get; set; }
        public DbSet<SizeRegionVersion> SizeRegionVersions { get; set; }

        public DbSet<PluginConfigurationValue> PluginConfigurationValues { get; set; }
        public DbSet<PluginConfigurationValueVersion> PluginConfigurationValueVersions { get; set; }
        public DbSet<PluginPermission> PluginPermissions { get; set; }
        public DbSet<PluginGroupPermission> PluginGroupPermissions { get; set; }
        public DbSet<PluginGroupPermissionVersion> PluginGroupPermissionVersions { get; set; }
    }
}
