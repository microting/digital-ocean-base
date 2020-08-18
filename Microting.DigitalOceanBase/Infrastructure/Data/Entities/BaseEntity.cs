using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microting.DigitalOceanBase.Configuration;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;
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
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
            Version = 1;
            WorkflowState = Constants.WorkflowStates.Created;

            await dbContext.AddAsync(this);
            await dbContext.SaveChangesAsync();

            var res = MapVersion(this);
            if (res != null)
            {
                await dbContext.AddAsync(res);
                await dbContext.SaveChangesAsync();
            }
        }

        public async Task Update(DigitalOceanDbContext dbContext)
        {
            await UpdateInternal(dbContext);
        }

        public async Task Delete(DigitalOceanDbContext dbContext)
        {
            await UpdateInternal(dbContext, Constants.WorkflowStates.Removed);
        }

        private async Task UpdateInternal(DigitalOceanDbContext dbContext, string state = null)
        {
            if (state != null)
            {
                WorkflowState = state;
            }

            if (dbContext.ChangeTracker.HasChanges())
            {
                Version += 1;
                UpdatedAt = DateTime.UtcNow;

                await dbContext.SaveChangesAsync();

                var res = MapVersion(this);
                if (res != null)
                {
                    await dbContext.AddAsync(res);
                    await dbContext.SaveChangesAsync();
                }
            }
        }

        private object MapVersion(object obj)
        {
            Type type = obj.GetType().UnderlyingSystemType;
            String className = type.Name;
            var name = obj.GetType().FullName + "Version";
            var resultType = Assembly.GetExecutingAssembly().GetType(name);
            if (resultType == null)
                return null;

            return Mapper.Map(obj, className.GetType(), resultType);
        }
    }
}
