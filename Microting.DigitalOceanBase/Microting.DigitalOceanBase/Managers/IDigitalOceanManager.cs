using System.Threading.Tasks;

namespace Microting.DigitalOceanBase.Managers
{
    public interface IDigitalOceanManager
    {
        Task GetDroplets();
    }
}