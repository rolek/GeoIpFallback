using System.Configuration.Provider;
using Sitecore.CES.GeoIp.Core.Model;

namespace GeoIpFallback.Mock
{
    public abstract class MockLocationFallbackProviderBase : ProviderBase
    {
        public abstract WhoIsInformation GetMockCurrentLocation();
    }
}
