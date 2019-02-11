using AutoMapper;
using ColdChainTrack.Auth.Models;
using ColdChainTrack.Auth.Utils;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Http;
using static ColdChainTrack.Auth.Models.BindingModels;
using static ColdChainTrack.Auth.Models.ViewModels;

namespace ColdChainTrack.Auth.Controllers
{
    [RoutePrefix("api/tracking")]
    public class TrackingController : ApiController
    {
        ApplicationDbContext dbContext = new ApplicationDbContext();

        [HttpGet]
        public HttpResponseMessage GetAlltracking()
        {
            try
            {
                List<TrackingViewModel> response = new List<TrackingViewModel>();
                var currentTrackingList = dbContext.Trackings.ToList();
                Mapper.Map(currentTrackingList, response);
                return Request.CreateResponse(HttpStatusCode.OK, response);
            }
            catch (Exception e)
            {
                var errors = ExceptionHandlerLogger.LogInnerMessages(e);
                return Request.CreateResponse(HttpStatusCode.Conflict, errors);
            }
        }

        [HttpGet]
        [Route("last")]
        public HttpResponseMessage GetLastRecord(GetLastTempertureRequest model)
        {
            try
            {
                TrackingViewModel response = new TrackingViewModel();
                var currentTracking = dbContext.Trackings
                    .Where(x => x.Device.Family == model.family && x.Device.Name == model.device)
                    .OrderByDescending(desc => desc.Dtm).ToList().Take(1);
                Mapper.Map(currentTracking, response);
                return Request.CreateResponse(HttpStatusCode.OK, response);
            }
            catch (Exception e)
            {
                var errors = ExceptionHandlerLogger.LogInnerMessages(e);
                return Request.CreateResponse(HttpStatusCode.Conflict, errors);
            }
        }

        [HttpGet]
        [Route("groupby")]
        public HttpResponseMessage GetAlltrackingGroupBy()
        {
            try
            {
                var currentTrackings = dbContext.Trackings.GroupBy(x => new
                {
                    x.Location,
                    x.Dtm,
                    x.Temperature,
                    x.Latitude,
                    x.Longitude,
                    x.Device
                })
                .Select(g => new { g.Key.Location, g.Key.Dtm, g.Key.Temperature, g.Key.Latitude, g.Key.Longitude, g.Key.Device.Name, g.Key.Device.Family })
                .OrderBy(o => o.Dtm).ToList();

                return Request.CreateResponse(HttpStatusCode.OK, currentTrackings);
            }
            catch (Exception e)
            {
                var errors = ExceptionHandlerLogger.LogInnerMessages(e);
                return Request.CreateResponse(HttpStatusCode.Conflict, errors);
            }
        }
    }
}