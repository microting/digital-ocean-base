using Microsoft.EntityFrameworkCore;
using Microting.DigitalOceanBase.Infrastructure.Constants;
using Microting.DigitalOceanBase.Infrastructure.Data;
using Microting.DigitalOceanBase.Infrastructure.Data.Entities;
using Microting.eFormApi.BasePn.Infrastructure.Database.Entities;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Castle.Core.Internal;

namespace Microting.DigitalOceanBase.UnitTests
{
    [TestFixture]
    public abstract class DbTestFixture
    {
        protected DigitalOceanDbContext DbContext { get; private set; }

        [SetUp]
        protected async Task SetUp()
        {
            DbContext = new DigitalOceanDbContextFactory().CreateDbContext(new string[] { });
            await DbContext.PluginConfigurationValues.AddAsync(
                new PluginConfigurationValue()
                {
                    Name= "MyMicrotingSettings:DigitalOceanToken"
                });
            await DbContext.SaveChangesAsync();
            await ClearDb();
        }

        [TearDown]
        public async Task TearDown()
        {
            await DbContext.DisposeAsync();
        }

        private async Task ClearDb()
        {
            List<string> modelNames = new List<string>
            {
                "PluginConfigurationValues",
                "Droplets",
                "DropletVersions",
                "DropletTag",
                "DropletTagVersions",
                "Images",
                "ImageVersions",
                "Regions",
                "SizeRegion",
                "SizeRegionVersions",
                "Sizes",
                "SizeVersions",
                "Tags"
            };
            bool firstRunNotDone = true;

            foreach (var modelName in modelNames)
            {
                try
                {
                    if (firstRunNotDone)
                    {
                        await DbContext.Database.ExecuteSqlRawAsync(
                            $"SET FOREIGN_KEY_CHECKS = 0;TRUNCATE `dobasedb`.`{modelName}`");
                    }
                }
                catch (Exception ex)
                {
                    if (ex.Message == "Unknown database 'dobasedb'")
                    {
                        firstRunNotDone = false;
                    }
                    else
                    {
                        Console.WriteLine(ex.Message);
                    }
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
            Assert.AreEqual(item.UpdatedByUserId, userId);
            Assert.AreNotEqual(item.CreatedAt, default(DateTime));
            Assert.AreNotEqual(item.UpdatedAt, default(DateTime));
            Assert.AreEqual(item.WorkflowState, WorkflowStates.Created);
        }

        protected static void CheckBaseUpdateInfo(int userId, BaseEntity item, int version = 1)
        {
            Assert.AreEqual(version,item.Version);
            Assert.AreEqual(userId, item.CreatedByUserId);
            Assert.AreEqual(userId, item.UpdatedByUserId);
            Assert.AreNotEqual(default(DateTime), item.CreatedAt);
            Assert.AreNotEqual(default(DateTime),item.UpdatedAt);
            Assert.AreEqual("created", item.WorkflowState);
        }

        protected static void CheckBaseRemoveInfo(int userId, BaseEntity item)
        {
            Assert.AreEqual(2, item.Version);
            Assert.AreEqual(userId, item.CreatedByUserId );
            Assert.AreEqual(userId, item.UpdatedByUserId);
            Assert.AreNotEqual(default(DateTime), item.CreatedAt);
            Assert.AreNotEqual(default(DateTime), item.UpdatedAt);
            Assert.AreEqual(WorkflowStates.Removed, item.WorkflowState);
        }
    }
}
