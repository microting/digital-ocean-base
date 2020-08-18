using System;
using System.Collections.Generic;
using System.Text;
using DigitalOcean.API.Models.Requests;

namespace Microting.DigitalOceanBase.Infrastructure.Api.Clients.Requests
{
    public class CreateDropletRequest
    {
        public string Name { get; set; }
        public string Region { get; set; }
        public string Size { get; set; }
        public object Image { get; set; }
        public List<object> SshKeys { get; set; }
        public bool? Backups { get; set; }
        public bool? Ipv6 { get; set; }
        public bool? PrivateNetworking { get; set; }
        public bool? Monitoring { get; set; }
        public string UserData { get; set; }
        public List<string> Tags { get; set; }
        public List<string> Volumes { get; set; }
        public string VpcUuid { get; set; }

        public Droplet ToDroplet()
        {
            return new Droplet()
            {
                Backups = this.Backups,
                Image = this.Image,
                Ipv6 = this.Ipv6,
                Monitoring = Monitoring,
                Name = Name,
                PrivateNetworking = PrivateNetworking,
                Region = Region,
                Size = Size,
                SshKeys = SshKeys,
                Tags = Tags,
                Volumes = Volumes,
                UserData = UserData,
                VpcUuid = VpcUuid
            };
        }
    }
}
