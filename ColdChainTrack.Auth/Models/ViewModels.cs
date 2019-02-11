using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ColdChainTrack.Auth.Models
{
    public class ViewModels
    {
        public class TrackingViewModel
        {
            public int Id { get; set; }
            public DateTime Dtm { get; set; }
            public int DeviceIdDevice { get; set; }
            public int Temperature { get; set; }
            public string Location { get; set; }
            public double Latitude { get; set; }
            public double Longitude { get; set; }
        }

        public class TrackingResumenViewModel
        {
            public DateTime Dtm { get; set; }
            public int Temperature { get; set; }
        }

        public class LocationViewModel
        {
            public string LocationName { get; set; }
            public List<DeviceViewModel> Devices { get; set; }

            public LocationViewModel()
            {
                Devices = new List<DeviceViewModel>();
            }
        }

        public class DeviceViewModel
        {
            public string DeviceName { get; set; }
            public List<TrackingResumenViewModel> Trackings { get; set; }

            public DeviceViewModel()
            {
                Trackings = new List<TrackingResumenViewModel>();
            }
        }
    }
}