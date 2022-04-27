#region Usings

using System;
using System.Net;
using GeoIpFallback.Mock;
using GeoIpFallback.Providers;
using Microsoft.Extensions.DependencyInjection;
using Sitecore.Analytics;
using Sitecore.Analytics.Pipelines.StartTracking;
using Sitecore.CES.GeoIp.Core;
using Sitecore.CES.GeoIp.Core.IpHashing;
using Sitecore.CES.GeoIp.Core.Model;
using Sitecore.Configuration;
using Sitecore.DependencyInjection;
using Sitecore.Diagnostics;

#endregion

namespace GeoIpFallback.Processors
{
    public class UpdateGeoIpData : StartTrackingProcessor
    {
        private const string CustomValuesKey = "GeoIpFallback.Mocks.Enabled";
        private IGeoIpManager _geoIpManager;

        private IGeoIpManager GeoIpManager =>
            _geoIpManager ?? (_geoIpManager = ServiceLocator.ServiceProvider.GetRequiredService<IGeoIpManager>());


        private bool IsMockEnabled
        {
            get
            {
                if (Tracker.Current.Session.Interaction.CustomValues.ContainsKey(CustomValuesKey))
                {
                    return (bool)Tracker.Current.Session.Interaction.CustomValues[CustomValuesKey];
                }

                var isEnabled = Settings.GetBoolSetting("GeoIpFallback.Mocks.Enabled", false);

                Tracker.Current.Session.Interaction.CustomValues.Add(CustomValuesKey, isEnabled);

                return isEnabled;
            }
        }

        public override void Process(StartTrackingArgs args)
        {
            Assert.IsNotNull(Tracker.Current, "Tracker.Current is not initialized");
            Assert.IsNotNull(Tracker.Current.Session, "Tracker.Current.Session is not initialized");

            if (Tracker.Current.Session.Interaction == null)
            {
                return;
            }

            if (IsMockEnabled)
            {
                var mockWhoIsInformation = MockLocationFallbackManager.MockLocationFallbackProvider.GetMockCurrentLocation();
                Tracker.Current.Session.Interaction.SetWhoIsInformation(mockWhoIsInformation);
                return;
            }

            var ip = GetIpAddress(Tracker.Current.Session.Interaction.Ip);
            var stringIp = ip.ToString();

            if (Tracker.Current.Session.Interaction.CustomValues.ContainsKey(stringIp) && UpdateGeoIpDataOverriden(ip))
            {
                Log.Debug("GeoIPFallback: the fallback version is overrided by data from Sitecore GEO IP service.", this);
                Tracker.Current.Session.Interaction.CustomValues.Remove(stringIp);
                return;
            }

            if (!Tracker.Current.Session.Interaction.HasGeoIpData)
            {
                try
                {
                    Log.Info(
                        "GeoIPFallback: Current location was not resolved by Sitecore GEO IP service; Local MaxMind database is requested. IP: " +
                        stringIp, this);

                    var whoIsInformation = LocationFallbackManager.LocationFallbackProvider.Resolve(ip);

                    Tracker.Current.Session.Interaction.SetWhoIsInformation(whoIsInformation);
                    Tracker.Current.Session.Interaction.CustomValues.Add(stringIp, whoIsInformation);
                }
                catch (Exception ex)
                {
                    Log.Error("UpdateGeoIpData: Something was wrong.", this);
                    Log.Error("Exception:", ex, this);
                }
            }
        }

        private bool UpdateGeoIpDataOverriden(IPAddress ip)
        {
            return UpdateGeoIpDataOverriden(new TimeSpan(0, 0, 0, 0, 0), ip);
        }

        private bool UpdateGeoIpDataOverriden(TimeSpan timeout, IPAddress ip)
        {
            var geoIpData = GeoIpManager.GetGeoIpData(ip.ToString(), timeout == TimeSpan.MaxValue ? TimeSpan.Zero : timeout);

            if (geoIpData.Status != GeoIpFetchDataStatus.Fetched || geoIpData.WhoIsInformation == null)
            {
                return false;
            }

            Tracker.Current.Session.Interaction.SetWhoIsInformation(geoIpData.WhoIsInformation);
            Tracker.Current.Session.Interaction.UpdateLocationReference();
            return true;
        }

        private IPAddress GetIpAddress(byte[] ip)
        {
            if (ip == null)
            {
                ip = IpHashProviderBase.EmptyIpAddress;
            }

            return new IPAddress(ip);
        }
    }
}