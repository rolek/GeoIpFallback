using System.Configuration.Provider;
using System.Net;
using Sitecore.CES.GeoIp.Core.Model;

namespace GeoIpFallback.Providers
{
    public abstract class LocationFallbackProviderBase : ProviderBase
    {
        public abstract WhoIsInformation Resolve(IPAddress ip);
    }
}