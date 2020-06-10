using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microting.DigitalOceanBase.Configuration;
using Microting.DigitalOceanBase.Infrastructure.Constants;
using Microting.DigitalOceanBase.Infrastructure.Data;
using Microting.DigitalOceanBase.Infrastructure.Data.Entities;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microting.DigitalOceanBase.UnitTests
{
    public class BaseTest
    {
        public Mapper Mapper { get; private set; }
        public DigitalOceanDbContext DbContext { get; private set; }

        [SetUp]
        protected async Task SetUp()
        {
            Mapper = new Mapper(AutomaperConfiguration.MapperConfiguration);
            DbContext = new DigitalOceanDbContextFactory().CreateDbContext(null);
            await DbContext.PluginConfigurationValues.AddAsync(
                new PluginConfigurationValues() 
                {
                    Name= "DigitalOceanBaseSettings:DigitalOceanToken"
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
            List<string> modelNames = new List<string>();
            modelNames.Add("PluginConfigurationValues");
            modelNames.Add("Droplets");
            modelNames.Add("DropletTag");
            modelNames.Add("Images");
            modelNames.Add("Regions");
            modelNames.Add("SizeRegion");
            modelNames.Add("Sizes");
            modelNames.Add("Tags");

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
                .CreateDbContext(null)
                .Database
                .MigrateAsync();
        }

        [OneTimeTearDown]
        public async Task OneTimeTearDown()
        {
            await new DigitalOceanDbContextFactory()
            .CreateDbContext(null)
            .Database.EnsureDeletedAsync();
        }


        public static void ChekcBaseCreateInfo(int userId, BaseEntity item)
        {
            Assert.AreEqual(item.Version, 1);
            Assert.AreEqual(item.CreatedByUserId, userId);
            Assert.AreEqual(item.UpdatedByUserId, 0);
            Assert.AreNotEqual(item.CreatedAt, default(DateTime));
            Assert.AreNotEqual(item.UpdatedAt, default(DateTime));
            Assert.AreEqual(item.WorkflowState, WorkflowStates.Created);
        }

        public static void ChekcBaseUpdateInfo(int userId, BaseEntity item)
        {
            Assert.AreEqual(item.Version, 2);
            Assert.AreEqual(item.CreatedByUserId, userId);
            Assert.AreEqual(item.UpdatedByUserId, userId);
            Assert.AreNotEqual(item.CreatedAt, default(DateTime));
            Assert.AreNotEqual(item.UpdatedAt, default(DateTime));
            Assert.AreEqual(item.WorkflowState, null);
        }

        public static void ChekcBaseRemoveInfo(int userId, BaseEntity item)
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