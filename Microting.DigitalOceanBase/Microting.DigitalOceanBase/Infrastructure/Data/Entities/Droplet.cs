using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Threading.Tasks;

namespace Microting.DigitalOceanBase.Infrastructure.Data.Entities
{
    public class Droplet : BaseEntity
    {
        public Droplet()
        {
            DropletTags = new List<DropletTag>();
        }

        public string DoUid { get; set; }
        public int CustomerNo { get; set; }//// leave it empty
        public string PublicIpV4 { get; set; }
        public string PrivateIpV4 { get; set; }
        public string PublicIpV6 { get; set; }
        public string CurrentImageName { get; set; }
        public string RequestedImageName { get; set; }// should be filled during rebuild
        public int CurrentImageId { get; set; } 
        public int RequestedImageId { get; set; }//// should be filled during rebuild
        public string UserData { get; set; } // leave it empty
        public bool MonitoringEnabled { get; set; }
        public bool IpV6Enabled { get; set; }
        public bool BackupsEnabled { get; set; }
        public Size Size { get; set; }
        public List<DropletTag> DropletTags { get; set; }

        [ForeignKey("Id")]
        public int Sizeid { get; set; }

        public override async Task Create(DigitalOceanDbContext dbContext)
        {
            CreatedAt = DateTime.Now;
            UpdatedAt = DateTime.Now;
            Version = 1;
            WorkflowState = Constants.WorkflowStates.Created;

            await dbContext.Droplets.AddAsync(this);
            await dbContext.SaveChangesAsync();
        }

        public override async Task Delete(DigitalOceanDbContext dbContext)
        {
            var  record = await dbContext.Droplets
                .FirstOrDefaultAsync(x => x.Id == Id);

            if (record == null)
                throw new NullReferenceException($"Could not find {this.GetType().Name} with ID: {Id}");

            record.WorkflowState = Constants.WorkflowStates.Removed;

            if (dbContext.ChangeTracker.HasChanges())
            {
                record.Id = 0;
                record.UpdatedAt = DateTime.UtcNow;
                record.UpdatedByUserId = UpdatedByUserId;
                record.Version += 1;

                await dbContext.Droplets.AddAsync(record);
                await dbContext.SaveChangesAsync();
            }
        }

        public override async Task Update(DigitalOceanDbContext dbContext)
        {
            var record = await dbContext.Droplets
                .FirstOrDefaultAsync(x => x.Id == Id);

            if (record == null)
                throw new NullReferenceException($"Could not find record { this.GetType().Name } with ID: {Id}");

            record = Mapper.Map<Droplet>(this);

            if (dbContext.ChangeTracker.HasChanges())
            {
                record.Id = 0;
                record.UpdatedAt = DateTime.UtcNow;
                record.UpdatedByUserId = UpdatedByUserId;
                record.Version += 1;

                await dbContext.Droplets.AddAsync(record);
                await dbContext.SaveChangesAsync();
            }
        }
    }
}
