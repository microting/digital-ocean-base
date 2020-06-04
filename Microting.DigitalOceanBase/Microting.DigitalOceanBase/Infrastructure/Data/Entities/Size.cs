using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microting.DigitalOceanBase.Infrastructure.Data.Entities
{
    public class Size: BaseEntity
    {
        public Droplet Droplet { get; set; }
        public string Slug { get; set; }
        public double Transfer { get; set; }
        public decimal PriceMonthly { get; set; }
        public decimal PriceHourly { get; set; }
        public int Memory { get; set; }
        public int Vcpus { get; set; }
        public int Disk { get; set; }
        public List<Region> Regions { get; set; }
        public bool Available { get; set; }

        public override async Task Create(DigitalOceanDbContext dbContext)
        {
            base.SetInitialCreateData();

            await dbContext.Sizes.AddAsync(this);
            await dbContext.SaveChangesAsync();
        }

        public override async Task Delete(DigitalOceanDbContext dbContext)
        {
            var record = await dbContext.Sizes
                .FirstOrDefaultAsync(x => x.Id == Id);

            if (record == null)
                throw new NullReferenceException($"Could not find {this.GetType().Name} with ID: {Id}");

            record.WorkflowState = Constants.WorkflowStates.Removed;

            if (dbContext.ChangeTracker.HasChanges())
            {
                SetUpdateDetails();

                await dbContext.Sizes.AddAsync(record);
                await dbContext.SaveChangesAsync();
            }
        }

        public override async Task Update(DigitalOceanDbContext dbContext)
        {
            var record = await dbContext.Sizes
                .FirstOrDefaultAsync(x => x.Id == Id);

            if (record == null)
                throw new NullReferenceException($"Could not find record { this.GetType().Name } with ID: {Id}");

            Mapper.Map(this, record);

            if (dbContext.ChangeTracker.HasChanges())
            {
                SetUpdateDetails();

                await dbContext.Sizes.AddAsync(record);
                await dbContext.SaveChangesAsync();
            }
        }
    }
}
