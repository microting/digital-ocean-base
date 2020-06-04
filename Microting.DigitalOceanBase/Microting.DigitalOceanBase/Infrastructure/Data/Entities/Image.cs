using AutoMapper;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microting.DigitalOceanBase.Infrastructure.Data.Entities
{
    public class Image : BaseEntity
    {
        public int DoUid { get; set; }
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


        public override async Task Create(DigitalOceanDbContext dbContext)
        {
            base.SetInitialCreateData();

            await dbContext.Images.AddAsync(this);
            await dbContext.SaveChangesAsync();
        }

        public override async Task Delete(DigitalOceanDbContext dbContext)
        {
            var record = await dbContext.Images
                .FirstOrDefaultAsync(x => x.Id == Id);

            if (record == null)
                throw new NullReferenceException($"Could not find {this.GetType().Name} with ID: {Id}");

            record.WorkflowState = Constants.WorkflowStates.Removed;

            if (dbContext.ChangeTracker.HasChanges())
            {
                record.UpdatedAt = DateTime.UtcNow;
                record.UpdatedByUserId = UpdatedByUserId;
                record.Version += 1;

                await dbContext.Images.AddAsync(record);
                await dbContext.SaveChangesAsync();
            }
        }

        public override async Task Update(DigitalOceanDbContext dbContext)
        {
            var record = await dbContext.Images
                .FirstOrDefaultAsync(x => x.Id == Id);

            if (record == null)
                throw new NullReferenceException($"Could not find record { this.GetType().Name } with ID: {Id}");

            record = Mapper.Map<Image>(this);

            if (dbContext.ChangeTracker.HasChanges())
            {
                record.Id = 0;
                record.UpdatedAt = DateTime.UtcNow;
                record.UpdatedByUserId = UpdatedByUserId;
                record.Version += 1;

                await dbContext.Images.AddAsync(record);
                await dbContext.SaveChangesAsync();
            }
        }
    }
}
