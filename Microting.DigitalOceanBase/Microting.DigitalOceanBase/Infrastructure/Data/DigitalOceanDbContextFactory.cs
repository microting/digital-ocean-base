using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Microting.DigitalOceanBase.Infrastructure.Data
{
    public class DigitalOceanDbContextFactory : IDesignTimeDbContextFactory<DigitalOceanDbContext>
    {
        public DigitalOceanDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<DigitalOceanDbContext>();
            optionsBuilder.UseMySQL("Server = localhost; port = 3306; Database = dobasedb; user = root; Convert Zero Datetime = true;");

            return new DigitalOceanDbContext(optionsBuilder.Options);
        }
    }
}
