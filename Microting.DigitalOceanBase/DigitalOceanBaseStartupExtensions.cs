using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using Microting.DigitalOceanBase.Configuration;
using Microting.DigitalOceanBase.Infrastructure.Api.Clients;
using Microting.DigitalOceanBase.Infrastructure.Data;
using Microting.DigitalOceanBase.Managers;
using System;
using System.Linq;

namespace Microting.DigitalOceanBase
{
    public static class DigitalOceanBaseStartupExtensions
    {
        public static IServiceCollection AddDigitalOceanBaseServices(this IServiceCollection services)
        {
            services.AddScoped(sp => {
                var dbCtx = sp.GetService<DigitalOceanDbContext>();
                var mapper = sp.GetService<IMapper>();

                var cred = dbCtx.PluginConfigurationValues?.FirstOrDefault(t => t.Name == "MyMicrotingSettings:DigitalOceanToken");
                if (cred == null)
                    throw new NullReferenceException("DigitalOcean token is not found");

                return (cred.Value == "abc123456789abc") ? (IApiClient)new MockApiClient() : new ApiClient(mapper, cred.Value);
            });

            services.AddScoped<IDigitalOceanManager, DigitalOceanManager>();
            services.AddSingleton<IMapper>(new Mapper(AutomaperConfiguration.MapperConfiguration));
            return services;
        }
    }
}
