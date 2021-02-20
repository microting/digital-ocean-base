using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;

namespace Microting.DigitalOceanBase.Infrastructure.Data
{
    public class DigitalOceanDbContextFactory : IDesignTimeDbContextFactory<DigitalOceanDbContext>
    {
        public DigitalOceanDbContext CreateDbContext(string[] args)
        {
            var defaultCs = "Server = localhost; port = 3306; Database = dobasedb; user = root; password = secretpassword; Convert Zero Datetime = true;";
            var optionsBuilder = new DbContextOptionsBuilder<DigitalOceanDbContext>();
            optionsBuilder.UseMySql(args.Any() ? args[0] : defaultCs, mysqlOptions =>
            {
                mysqlOptions.ServerVersion(new Version(10, 4, 0), ServerType.MariaDb).EnableRetryOnFailure();
            });
            return new DigitalOceanDbContext(optionsBuilder.Options);
            // dotnet ef migrations add InitialCreate --project Microting.DigitalOceanBase --startup-project DBMigrator
        }
    }
}
