using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microting.DigitalOceanBase.Infrastructure.Api.Clients.Requests;
using Microting.DigitalOceanBase.Managers;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Microting.DigitalOceanBase.App
{
    class Program
    {
        static void Main(string[] args)
        {
            var configuration = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(AppContext.BaseDirectory))
            .AddJsonFile("appsettings.json", optional: true)
            .Build();


            var serviceProvider = new ServiceCollection()
               .AddDigitalOceanBaseServices(configuration.GetConnectionString("DigitalOceanDb"))
               .BuildServiceProvider();

            var manager = serviceProvider.GetService<IDigitalOceanManager>();
            try
            {
                Task.WaitAll(manager.FetchDropletsAsync(11));
                //Task.WaitAll(manager.RebuildDropletAsync(11, 1, 1));
                //Task.WaitAll(manager.CreateDropletAsync(11, new CreateDropletRequest() {
                //    Name = "MyTestImage",
                //    Region = "nyc3",
                //    Size = "s-1vcpu-1gb",
                //    Image = "ubuntu-16-04-x64"
                //}));
            }
            catch (Exception ex)
            {

                throw;
            }
           
            //Console.WriteLine("Done");
            //Console.ReadLine();
        }
    }
}
