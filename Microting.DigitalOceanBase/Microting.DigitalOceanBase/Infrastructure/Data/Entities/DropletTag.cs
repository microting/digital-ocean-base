using AutoMapper;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace Microting.DigitalOceanBase.Infrastructure.Data.Entities
{
    public class DropletTag : BaseEntity
    {
        public int DropletId { get; set; }
        public int TagId { get; set; }

        public override async Task Create(DigitalOceanDbContext dbContext)
        {
            base.SetInitialCreateData();

            await dbContext.DropletTag.AddAsync(this);
            await dbContext.SaveChangesAsync();
        }

        public override async Task Delete(DigitalOceanDbContext dbContext)
        {
            var record = await dbContext.DropletTag
                .FirstOrDefaultAsync(x => x.Id == Id);

            if (record == null)
                throw new NullReferenceException($"Could not find {this.GetType().Name} with ID: {Id}");

            record.WorkflowState = Constants.WorkflowStates.Removed;

            if (dbContext.ChangeTracker.HasChanges())
            {
                record.UpdatedAt = DateTime.UtcNow;
                record.UpdatedByUserId = UpdatedByUserId;
                record.Version += 1;

                await dbContext.DropletTag.AddAsync(record);
                await dbContext.SaveChangesAsync();
            }
        }

        public override async Task Update(DigitalOceanDbContext dbContext)
        {
            var record = await dbContext.DropletTag
                .FirstOrDefaultAsync(x => x.Id == Id);

            if (record == null)
                throw new NullReferenceException($"Could not find record { this.GetType().Name } with ID: {Id}");

            record = Mapper.Map<DropletTag>(this);

            if (dbContext.ChangeTracker.HasChanges())
            {
                record.UpdatedAt = DateTime.UtcNow;
                record.UpdatedByUserId = UpdatedByUserId;
                record.Version += 1;

                await dbContext.DropletTag.AddAsync(record);
                await dbContext.SaveChangesAsync();
            }
        }
    }
}
