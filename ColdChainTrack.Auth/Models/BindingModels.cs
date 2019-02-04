using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ColdChainTrack.Auth.Models
{
    public class BindingModels
    {
        public class GetLastTempertureRequest
        {
            public string family { get; set; }
            public string device { get; set; }
        }
    }
}