using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microting.DigitalOceanBase.Configuration;
using Microting.DigitalOceanBase.Infrastructure.Constants;
using Microting.DigitalOceanBase.Infrastructure.Data;
using Microting.DigitalOceanBase.Infrastructure.Data.Entities;
using Microting.eFormApi.BasePn.Infrastructure.Database.Entities;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microting.DigitalOceanBase.UnitTests
{
    [TestFixture]
    public abstract class DbTestFixture
    {
        protected Mapper Mapper { get; private set; }
        protected DigitalOceanDbContext DbContext { get; private set; }

        [SetUp]
        protected async Task SetUp()
        {
            Mapper = new Mapper(AutomaperConfiguration.MapperConfiguration);
            DbContext = new DigitalOceanDbContextFactory().CreateDbContext(new string[] { });
            await DbContext.PluginConfigurationValues.AddAsync(
                new PluginConfigurationValue() 
                {
                    Name= "MyMicrotingSettings:DigitalOceanToken"
                });
            await DbContext.SaveChangesAsync();
        }

        [TearDown]
        public async Task TearDown()
        {
            await ClearDb();
            await DbContext.DisposeAsync();
        }

        private async Task ClearDb()
        {
            List<string> modelNames = new List<string>
            {
                "PluginConfigurationValues",
                "Droplets",
                "DropletTag",
                "Images",
                "Regions",
                "SizeRegion",
                "Sizes",
                "Tags"
            };

            foreach (var modelName in modelNames)
            {
                try
                {
                    var sqlCmd = $"SET FOREIGN_KEY_CHECKS = 0;TRUNCATE `{modelName}`";
                    await DbContext.Database.ExecuteSqlRawAsync(sqlCmd);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

        [OneTimeSetUp]
        public async Task OneTimeSetUp()
        {
            await new DigitalOceanDbContextFactory()
                .CreateDbContext(new string[] { })
                .Database
                .MigrateAsync();
        }

        [OneTimeTearDown]
        public async Task OneTimeTearDown()
        {
            await new DigitalOceanDbContextFactory()
            .CreateDbContext(new string[] { })
            .Database.EnsureDeletedAsync();
        }


        protected static void CheckBaseCreateInfo(int userId, BaseEntity item)
        {
            Assert.AreEqual(item.Version, 1);
            Assert.AreEqual(item.CreatedByUserId, userId);
            Assert.AreEqual(item.UpdatedByUserId, 0);
            Assert.AreNotEqual(item.CreatedAt, default(DateTime));
            Assert.AreNotEqual(item.UpdatedAt, default(DateTime));
            Assert.AreEqual(item.WorkflowState, WorkflowStates.Created);
        }

        protected static void CheckBaseUpdateInfo(int userId, BaseEntity item, int version = 2)
        {
            Assert.AreEqual(item.Version, version);
            Assert.AreEqual(item.CreatedByUserId, userId);
            Assert.AreEqual(item.UpdatedByUserId, userId);
            Assert.AreNotEqual(item.CreatedAt, default(DateTime));
            Assert.AreNotEqual(item.UpdatedAt, default(DateTime));
            Assert.AreEqual(item.WorkflowState, null);
        }

        protected static void CheckBaseRemoveInfo(int userId, BaseEntity item)
        {
            Assert.AreEqual(item.Version, 2);
            Assert.AreEqual(item.CreatedByUserId, userId);
            Assert.AreEqual(item.UpdatedByUserId, userId);
            Assert.AreNotEqual(item.CreatedAt, default(DateTime));
            Assert.AreNotEqual(item.UpdatedAt, default(DateTime));
            Assert.AreEqual(item.WorkflowState, WorkflowStates.Removed);
        }
    }
}