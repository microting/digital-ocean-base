using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microting.DigitalOceanBase.Managers;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Microting.DigitalOceanBase.App
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var configuration = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(AppContext.BaseDirectory))
            .AddJsonFile("appsettings.json", optional: true)
            .Build();


            var serviceProvider = new ServiceCollection()
               .AddDigitalOceanBaseServices(configuration.GetConnectionString("DigitalOceanDb"))
               .BuildServiceProvider();

            var manager = serviceProvider.GetService<IDigitalOceanManager>();

            await manager.GetDroplets();

            Console.ReadLine();
        }
    }
}
