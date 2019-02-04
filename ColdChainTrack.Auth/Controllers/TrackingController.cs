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

        [HttpGet]
        [Route("retriveTemperature")]
        public HttpResponseMessage RetrieveTemperature(int deviceId, string family)
        {

            var obj = dbContext.Devices.FirstOrDefault(d => d.IdDevice == deviceId && d.Family == family);

            if (obj == null)
            {
                return Request.CreateResponse(HttpStatusCode.NotFound);
            }

            return Request.CreateResponse(HttpStatusCode.OK, obj);
        }

        //[HttpPost]
        //[Route("save")]
        //public async Task<HttpResponseMessage> Save(TrackLocation tl) {

        //    var client = new HttpClient();
        //    string url = Constants.UrlGo;
        //    string json = JsonConvert.SerializeObject(tl);

        //    var stringContent = new StringContent(json, Encoding.UTF8, "application/json");
        //    HttpResponseMessage response = await client.PostAsync($"{url}/track", stringContent);

        //    if (response.IsSuccessStatusCode)
        //    {
        //        var responseJson = await response.Content.ReadAsStringAsync();
        //        GoApiResponse apiResponse = JsonConvert.DeserializeObject<GoApiResponse>(responseJson);
        //        SaveInDbData(apiResponse, tl);
        //        return Request.CreateResponse(HttpStatusCode.OK);
        //    }
        //    return Request.CreateResponse(HttpStatusCode.Conflict);
        //}

        /// <summary>
        /// Save Tracking for a device
        /// </summary>
        /// <param name="guessPlace">Name guessed by golagn server</param>
        /// <param name="tl">track location object</param>
        /// <returns>Ok if data were saved</returns>
        [HttpPost]
        [Route("save")]
        public async Task<HttpResponseMessage> Save([FromUri]string guessPlace, [FromBody]TrackLocation tl)
        {
            //var context = new ColdChainTrackerContext();
            var device = dbContext.Devices.FirstOrDefault(d => d.Name == tl.d);
            if (device == null)
            {
                device = new Device { Family = tl.f, Name = tl.d };
                dbContext.Devices.Add(device);
                await dbContext.SaveChangesAsync();
            }

            Tracking tracking = new Tracking
            {
                Device = device,
                Location = guessPlace,
                Temperature = tl.s.temperature["sensor"],
                Dtm = UnixTimeStampToDateTime(tl.t),
            };

            LocationBasicResponse LocationBasicResponse = LocationBasic(Constants.UrlGo, tl);
            if (LocationBasicResponse != null && LocationBasicResponse.Success)
            {
                tracking.Latitude = LocationBasicResponse.Data.Gps.Lat;
                tracking.Longitude = LocationBasicResponse.Data.Gps.Lon;
            }
            dbContext.Trackings.Add(tracking);
            await dbContext.SaveChangesAsync();
            return Request.CreateResponse(HttpStatusCode.OK);
        }

        private LocationBasicResponse LocationBasic(string url, TrackLocation t1)
        {
            HttpClient client = new HttpClient();
            HttpResponseMessage response = client.GetAsync($"{url}/api/v1/location_basic/{t1.f}/{t1.d}").Result;
            var LocationBR = Newtonsoft.Json.JsonConvert.DeserializeObject<LocationBasicResponse>(response.Content.ReadAsStringAsync().Result);
            return LocationBR;
        }

        private DateTime UnixTimeStampToDateTime(long unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddSeconds(unixTimeStamp).ToLocalTime();
            return dtDateTime;
        }

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