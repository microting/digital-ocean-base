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
                // images sync ok
                // droplet sync - to do
                // create droplet - ok,  // check flags, ssh keys, change sizes and regions
                // rebuild droplet  - to do

                //Task.WaitAll(manager.FetchDropletsAsync(11));
                Task.WaitAll(manager.RebuildDropletAsync(11, 194408182, 64531677));
                //Task.WaitAll(manager.CreateDropletAsync(11, new CreateDropletRequest()
                //{
                //    Name = "MyTestImage",
                //    Region = "nyc3",
                //    Size = "s-1vcpu-1gb",
                //    Image = "ubuntu-16-04-x64",
                //    Tags =  new System.Collections.Generic.List<string>() { "test", "Test2"},
                //    Ipv6 = true,
                //    PrivateNetworking = true, 
                //    Monitoring = true,
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
