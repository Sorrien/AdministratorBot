using System.Linq;
using System.Net.NetworkInformation;

namespace AdministratorBot.Infrastructure
{
    public interface IIPWrapper
    {
        bool IsPortInUse(int port);
    }
    public class IPWrapper : IIPWrapper
    {
        public bool IsPortInUse(int port)
        {
            var listeners = IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners().ToList();
            listeners.AddRange(IPGlobalProperties.GetIPGlobalProperties().GetActiveUdpListeners());
            var listener = listeners.FirstOrDefault(x => x.Port == port);
            return listeners.Any(x => x.Port == port);
        }
    }
}
