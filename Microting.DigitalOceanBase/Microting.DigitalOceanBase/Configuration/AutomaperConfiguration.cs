﻿using AutoMapper;
using Microting.DigitalOceanBase.Infrastructure.Api.Clients.Requests;
using Microting.DigitalOceanBase.Infrastructure.Data.Entities;
using System.Linq;

namespace Microting.DigitalOceanBase.Configuration
{
    internal static class AutomaperConfiguration
    {
        public static MapperConfiguration MapperConfiguration = new MapperConfiguration(cfg => {
            cfg.CreateMap<Tag, Tag>();
            cfg.CreateMap<DropletTag, DropletTag>();
            cfg.CreateMap<Droplet, Droplet>();
            cfg.CreateMap<DigitalOcean.API.Models.Responses.Droplet, Droplet>()
                .ForMember(t => t.Id, opts => opts.Ignore())
                .ForMember(t => t.DoUid, opts => opts.MapFrom(m => m.Id))
                .ForMember(t => t.CurrentImageId, opts => opts.MapFrom(m => m.Image.Id))
                .ForMember(t => t.CurrentImageName, opts => opts.MapFrom(m => m.Image.Name))
                .ForMember(t => t.IpV6Enabled, opts => opts.MapFrom(m => m.Features.Any(t => t == "ipv6")))
                .ForMember(t => t.BackupsEnabled, opts => opts.MapFrom(m => m.Features.Any(t => t == "backups")))
                .ForMember(t => t.MonitoringEnabled, opts => opts.MapFrom(m => m.Features.Any(t => t == "monitoring")))
                .ForMember(t => t.PublicIpV6, opts => opts.MapFrom(m => m.Networks.V6.FirstOrDefault(g => g.Type == "public") == null ? null : m.Networks.V6.FirstOrDefault(g => g.Type == "public").IpAddress))
                .ForMember(t => t.PrivateIpV4, opts => opts.MapFrom(m => m.Networks.V4.FirstOrDefault(g => g.Type != "public") == null ? null : m.Networks.V4.FirstOrDefault(g => g.Type != "public").IpAddress))
                .ForMember(t => t.PublicIpV4, opts => opts.MapFrom(m => m.Networks.V4.FirstOrDefault(g => g.Type == "public") == null ? null : m.Networks.V4.FirstOrDefault(g => g.Type == "public").IpAddress));
            cfg.CreateMap<DigitalOcean.API.Models.Responses.Size, Size>();
            cfg.CreateMap<string, Region>()
                .ForMember(t => t.Name, opts => opts.MapFrom(m => m));
            cfg.CreateMap<Image, Image>();
            cfg.CreateMap<CreateDropletRequest, DigitalOcean.API.Models.Requests.Droplet>();
        });
    }
}