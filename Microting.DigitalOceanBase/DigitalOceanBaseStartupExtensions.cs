using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using Microting.DigitalOceanBase.Configuration;
using Microting.DigitalOceanBase.Infrastructure.Api.Clients;
using Microting.DigitalOceanBase.Managers;

namespace Microting.DigitalOceanBase
{
    public static class DigitalOceanBaseStartupExtensions
    {
        public static IServiceCollection AddDigitalOceanBaseServices(this IServiceCollection services)
        {

            services.AddScoped<IApiClient, ApiClient>();
            services.AddScoped<IDigitalOceanManager, DigitalOceanManager>();
            services.AddSingleton<IMapper>(new Mapper(AutomaperConfiguration.MapperConfiguration));
            return services;
        }
    }
}
