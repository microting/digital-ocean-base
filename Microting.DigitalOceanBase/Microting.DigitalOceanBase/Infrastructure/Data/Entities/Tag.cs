using AutoMapper;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace Microting.DigitalOceanBase.Infrastructure.Data.Entities
{
    public class Tag : BaseEntity
    {
       public string Name { get; set; }

        public override async Task Create(DigitalOceanDbContext dbContext)
        {
            base.SetInitialCreateData();

            await dbContext.Tags.AddAsync(this);
            await dbContext.SaveChangesAsync();
        }

        public override async Task Delete(DigitalOceanDbContext dbContext)
        {
            var record = await dbContext.Tags
                .FirstOrDefaultAsync(x => x.Id == Id);

            if (record == null)
                throw new NullReferenceException($"Could not find {this.GetType().Name} with ID: {Id}");

            record.WorkflowState = Constants.WorkflowStates.Removed;

            if (dbContext.ChangeTracker.HasChanges())
            {
                record.UpdatedAt = DateTime.UtcNow;
                record.UpdatedByUserId = UpdatedByUserId;
                record.Version += 1;

                await dbContext.Tags.AddAsync(record);
                await dbContext.SaveChangesAsync();
            }
        }

        public override async Task Update(DigitalOceanDbContext dbContext)
        {
            var record = await dbContext.Tags
                .FirstOrDefaultAsync(x => x.Id == Id);

            if (record == null)
                throw new NullReferenceException($"Could not find record { this.GetType().Name } with ID: {Id}");

            record = Mapper.Map<Tag>(this);

            if (dbContext.ChangeTracker.HasChanges())
            {
                record.UpdatedAt = DateTime.UtcNow;
                record.UpdatedByUserId = UpdatedByUserId;
                record.Version += 1;

                await dbContext.Tags.AddAsync(record);
                await dbContext.SaveChangesAsync();
            }
        }
    }
}
