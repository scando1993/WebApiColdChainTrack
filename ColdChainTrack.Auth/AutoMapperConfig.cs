using AutoMapper;
using ColdChainTrack.Auth.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using static ColdChainTrack.Auth.Models.ViewModels;

namespace ColdChainTrack.Auth
{
    public class AutoMapperConfig
    {
        public static void Initialize()
        {
            Mapper.Initialize((config) =>
            {
                config.CreateMap<Tracking, TrackingViewModel>().ReverseMap();

            });
        }
    }
}