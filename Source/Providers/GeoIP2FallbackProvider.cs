#region Usings

using System.Collections.Specialized;
using System.Net;
using System.Web.Hosting;
using MaxMind.GeoIP2;
using Sitecore.CES.GeoIp.Core.Model;
using Sitecore.Diagnostics;

#endregion

namespace GeoIpFallback.Providers
{
    public class GeoIp2FallbackProvider : LocationFallbackProviderBase
    {
        private string _databasePath;

        public override void Initialize(string name, NameValueCollection config)
        {
            string databasePath = config["database"];
            if (!string.IsNullOrEmpty(databasePath))
            {
                _databasePath = databasePath;
            }
            else
            {
                _databasePath = "~/app_data/GeoLite2-City.mmdb";
            }

            base.Initialize(name, config);
        }

        public override WhoIsInformation Resolve(IPAddress ip)
        {
            var whoIsInformation = new WhoIsInformation();

            using (var reader = new DatabaseReader(HostingEnvironment.MapPath(_databasePath)))
            {
                var city = reader.City(ip);

                if (city != null)
                {
                    Log.Info("GeoIPFallback: current location was resolved by local MaxMind database.", this);

                    whoIsInformation.Country = city.Country.IsoCode;
                    Log.Debug("GeoIPFallback: Country: " + whoIsInformation.Country, this);

                    whoIsInformation.City = city.City.Name;
                    Log.Debug("GeoIPFallback: City: " + whoIsInformation.City, this);

                    whoIsInformation.PostalCode = city.Postal.Code;
                    Log.Debug("GeoIPFallback: Postal Code: " + whoIsInformation.PostalCode, this);

                    whoIsInformation.Latitude = city.Location.Latitude;
                    Log.Debug("GeoIPFallback: Latitude: " + whoIsInformation.Latitude, this);

                    whoIsInformation.Longitude = city.Location.Longitude;
                    Log.Debug("GeoIPFallback: Longitude: " + whoIsInformation.Longitude, this);

                    whoIsInformation.MetroCode = city.MostSpecificSubdivision.Name;
                    Log.Debug("GeoIPFallback: Metro Code: " + whoIsInformation.MetroCode, this);

                    whoIsInformation.AreaCode = city.MostSpecificSubdivision.IsoCode;
                    Log.Debug("GeoIPFallback: Area Code: " + whoIsInformation.AreaCode, this);
                }
                else
                {
                    Log.Info("GeoIPFallback: current location was not resolved by local MaxMind database.", this);
                    whoIsInformation.BusinessName = "Not Available";
                }
            }

            return whoIsInformation;
        }
    }
}