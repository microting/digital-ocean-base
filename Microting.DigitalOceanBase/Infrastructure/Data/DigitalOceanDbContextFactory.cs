using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System.Linq;

namespace Microting.DigitalOceanBase.Infrastructure.Data
{
    public class DigitalOceanDbContextFactory : IDesignTimeDbContextFactory<DigitalOceanDbContext>
    {
        public DigitalOceanDbContext CreateDbContext(string[] args)
        {
            var defaultCs = "Server = localhost; port = 3306; Database = dobasedb; user = root; Convert Zero Datetime = true;";
            var optionsBuilder = new DbContextOptionsBuilder<DigitalOceanDbContext>();
            optionsBuilder.UseMySql(args.Any() ? args[0]: defaultCs);

            return new DigitalOceanDbContext(optionsBuilder.Options);
            // dotnet ef migrations add InitialCreate --project Microting.DigitalOceanBase --startup-project DBMigrator
        }
    }
}
