using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microting.DigitalOceanBase.Configuration;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace Microting.DigitalOceanBase.Infrastructure.Data.Entities
{
    public abstract class BaseEntity
    {
        protected Mapper Mapper { get; private set; }

        public BaseEntity()
        {
            Mapper = new Mapper(AutomaperConfiguration.MapperConfiguration);
        }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        [StringLength(255)]
        public string WorkflowState { get; set; }
        public int CreatedByUserId { get; set; }
        public int UpdatedByUserId { get; set; }
        public int Version { get; set; }


        public async Task Create(DigitalOceanDbContext dbContext)
        {
            CreatedAt = DateTime.Now;
            UpdatedAt = DateTime.Now;
            Version = 1;
            WorkflowState = Constants.WorkflowStates.Created;

            await dbContext.AddAsync(this);
            await dbContext.SaveChangesAsync();
        }

        public async Task Update<T>(DigitalOceanDbContext dbContext) where T : BaseEntity
        {
            await UpdateInternal<T>(dbContext);
        }

        public async Task Delete<T>(DigitalOceanDbContext dbContext) where T : BaseEntity
        {
            await UpdateInternal<T>(dbContext, Constants.WorkflowStates.Removed);
        }

        private async Task UpdateInternal<T>(DigitalOceanDbContext dbContext, string state = null) where T : BaseEntity
        {
            using (var ctx = new DigitalOceanDbContextFactory().CreateDbContext(null))
            {
                var record = await ctx.Set<T>().FirstOrDefaultAsync(x => x.Id == Id);
                if (record == null)
                    throw new NullReferenceException($"Could not find {this.GetType().Name} with ID: {Id}");

                Mapper.Map(this, record);

                if (state != null)
                    record.WorkflowState = state;

                if (ctx.ChangeTracker.HasChanges())
                {
                    Id = 0;
                    UpdatedAt = DateTime.UtcNow;
                    UpdatedByUserId = UpdatedByUserId;
                    Version = record.Version + 1;
                    CreatedAt = record.CreatedAt;
                    CreatedByUserId = record.CreatedByUserId;

                    if (state != null)
                        WorkflowState = state;

                    await dbContext.AddAsync(this);
                    await dbContext.SaveChangesAsync();
                }
            }
        }
    }
}
