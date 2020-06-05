using AutoMapper;
using Microting.DigitalOceanBase.Configuration;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
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


        public abstract Task Create(DigitalOceanDbContext dbContext);

        public abstract Task Update(DigitalOceanDbContext dbContext);

        public abstract Task Delete(DigitalOceanDbContext dbContext);

        protected void SetInitialCreateData()
        {
            CreatedAt = DateTime.Now;
            UpdatedAt = DateTime.Now;
            Version = 1;
            WorkflowState = Constants.WorkflowStates.Created;
        }

        protected void SetUpdateDetails()
        {
            Id = 0;
            UpdatedAt = DateTime.UtcNow;
            UpdatedByUserId = UpdatedByUserId;
            Version += 1;
        }
    }
}
