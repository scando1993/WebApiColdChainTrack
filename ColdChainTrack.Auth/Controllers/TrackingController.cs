using AutoMapper;
using ColdChainTrack.Auth.Models;
using ColdChainTrack.Auth.Utils;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
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

        /// <summary>
        /// Get the last temperature register for a device
        /// </summary>
        /// <param name="name">device name</param>
        /// <param name="family">family name</param>
        /// <returns></returns>
        [HttpGet]
        [Route("getTemperature")]
        public HttpResponseMessage RetrieveTemperature(string name, string family)
        {

            var obj = dbContext.Devices.FirstOrDefault(d => d.Name == name && d.Family == family);

            if (obj == null)
            {
                return Request.CreateResponse(HttpStatusCode.NotFound);
            }

            return Request.CreateResponse(HttpStatusCode.OK, obj);
        }

        /// <summary>
        /// Save Tracking for a device
        /// </summary>
        /// <param name="guessPlace">Name guessed by golagn server</param>
        /// <param name="tl">track location object</param>
        /// <returns>Ok if data were saved</returns>
        [HttpPost]
        [Route("save")]
        public HttpResponseMessage Save(Tracking tracking)
        {
            dbContext.Trackings.Add(tracking);
            dbContext.SaveChanges();
            return Request.CreateResponse(HttpStatusCode.OK);
        }

        [HttpGet]
        [Route("all")]
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
                    x.Longitude
                })
                .Select(g => new { g.Key.Location, g.Key.Dtm, g.Key.Temperature, g.Key.Latitude, g.Key.Longitude })
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