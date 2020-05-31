using AutoMapper;
using Microting.DigitalOceanBase.Infrastructure.Api.Clients.Requests;
using Microting.DigitalOceanBase.Infrastructure.Data.Entities;

namespace Microting.DigitalOceanBase.Configuration
{
    internal static class AutomaperConfiguration
    {
        public static MapperConfiguration MapperConfiguration = new MapperConfiguration(cfg => {
            cfg.CreateMap<Tag, Tag>();
            cfg.CreateMap<DropletTag, DropletTag>();
            cfg.CreateMap<Droplet, Droplet>();
            cfg.CreateMap<Image, Image>();
            cfg.CreateMap<CreateDropletRequest, DigitalOcean.API.Models.Requests.Droplet>();
        });
    }
}
