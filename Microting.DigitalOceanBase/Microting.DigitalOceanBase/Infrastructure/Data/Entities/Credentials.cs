using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Microting.DigitalOceanBase.Infrastructure.Data.Entities
{
    public class Credential
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [MaxLength(265)]
        public string Key { get; set; }

        public string Value { get; set; }
    }
}
