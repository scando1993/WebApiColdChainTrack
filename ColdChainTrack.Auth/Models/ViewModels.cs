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
    }
}