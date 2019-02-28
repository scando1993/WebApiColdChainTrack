using AutoMapper;
using ColdChainTrack.Auth.Models;
using ColdChainTrack.Auth.Utils;
using Newtonsoft.Json;
using OfficeOpenXml;
//using NPOI.SS.UserModel;
//using NPOI.XSSF.UserModel;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
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
        [Route("trackAll")]
        public HttpResponseMessage RetrieveTemperature(string name, string family)
        {
            Device device = dbContext.Devices.FirstOrDefault(d => d.Name == name && d.Family == family);

            if (device == null)
            {
                return Request.CreateResponse(HttpStatusCode.NotFound);
            }
            List<Tracking> trackingList = dbContext.Trackings.Where(t => t.DeviceIdDevice == device.IdDevice).ToList();

            return Request.CreateResponse(HttpStatusCode.OK, trackingList);
        }
        /// <summary>
        /// Get all Families
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("getAllFamilies")]
        public HttpResponseMessage GetAllFamilies()
        {
            var families = dbContext.Devices.GroupBy(f => f.Family, (key, group) => new { family = key }).ToList();

            if (families == null)
            {
                return Request.CreateResponse(HttpStatusCode.NotFound);
            }

            return Request.CreateResponse(HttpStatusCode.OK, families);
        }

        /// <summary>
        /// Get list device from a family
        /// </summary>
        /// <param name="family">Family's name</param>
        /// <returns>device json if all it's OK or 404</returns>
        [HttpGet]
        [Route("getAllDevice")]
        public HttpResponseMessage RetrieveTemperature(string family)
        {
            var devices = dbContext.Devices.Where(d => d.Family == family).ToList();

            if (devices == null)
            {
                return Request.CreateResponse(HttpStatusCode.NotFound);
            }
            return Request.CreateResponse(HttpStatusCode.OK, devices);
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
        /// <summary>
        /// Add a device
        /// </summary>
        /// <param name="device"></param>
        /// <returns>Created</returns>
        [HttpPost]
        [Route("addDevice")]
        public HttpResponseMessage AddDevice(Device device)
        {
            dbContext.Devices.Add(device);
            dbContext.SaveChanges();
            return Request.CreateResponse(HttpStatusCode.Created);
        }

        /// <summary>
        /// Get all tracking rows without filtering
        /// </summary>
        /// <returns></returns>
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

        //[HttpPost]
        //[Route("last")]
        //public HttpResponseMessage GetLastRecord(GetLastTempertureRequest model)
        //{
        //    try
        //    {
        //        TrackingViewModel response = new TrackingViewModel();
        //        var currentTracking = dbContext.Trackings
        //            .Where(x => x.Device.Family == model.family && x.Device.Name == model.device)
        //            .OrderByDescending(desc => desc.Dtm).ToList().Take(1);
        //        Mapper.Map(currentTracking, response);
        //        return Request.CreateResponse(HttpStatusCode.OK, response);
        //    }
        //    catch (Exception e)
        //    {
        //        var errors = ExceptionHandlerLogger.LogInnerMessages(e);
        //        return Request.CreateResponse(HttpStatusCode.Conflict, errors);
        //    }
        //}

        [HttpGet]
        [Route("groupby")]
        public HttpResponseMessage GetAlltrackingGroupBy(string family)
        {
            try
            {
                //Se obtienen todos los devices por familia
                var devices = dbContext.Devices.Where(d => d.Family.Equals(family));

                //Se obtiene todos los trackings de los devices seleccionados
                var trackings = (from A in dbContext.Trackings
                                 join B in devices on A.DeviceIdDevice equals B.IdDevice
                                 select A).ToList();

                //Se agrupa los trackings por locations
                List<LocationViewModel> locations = new List<LocationViewModel>();
                foreach (var location in trackings.GroupBy(t => t.Location))
                {
                    LocationViewModel locationVM = new LocationViewModel();
                    List<DeviceViewModel> devicesVM = new List<DeviceViewModel>();

                    foreach (var device in trackings.Where(t => t.Location.Equals(location.Key)).GroupBy(t => t.Device))
                    {
                        devicesVM.Add(new DeviceViewModel
                        {
                            DeviceName = device.Key.Name,
                            Trackings = trackings.Where(t => t.Location.Equals(location.Key) && t.DeviceIdDevice.Equals(device.Key.IdDevice)).OrderBy(t => t.Dtm).Select(t => new TrackingResumenViewModel
                            {
                                Dtm = t.Dtm,
                                Temperature = t.Temperature
                            }).ToList()
                        });
                    }

                    locationVM.LocationName = location.Key;
                    locationVM.Devices.AddRange(devicesVM);
                    locations.Add(locationVM);
                }

                return Request.CreateResponse(HttpStatusCode.OK, locations);
            }
            catch (Exception e)
            {
                var errors = ExceptionHandlerLogger.LogInnerMessages(e);
                return Request.CreateResponse(HttpStatusCode.Conflict, errors);
            }
        }

        [HttpGet]
        [Route("groupby")]
        public HttpResponseMessage GetAlltrackingGroupBy(string location, string family)
        {
            try
            {
                //Se obtienen todos los devices por familia
                var devices = dbContext.Devices.Where(d => d.Family.Equals(family));

                //Se obtiene todos los trackings de los devices seleccionados
                var trackings = (from A in dbContext.Trackings
                                 join B in devices on A.DeviceIdDevice equals B.IdDevice
                                 select A).ToList();

                //Se agrupa los trackings por locations
                List<LocationViewModel> locations = new List<LocationViewModel>();

                LocationViewModel locationVM = new LocationViewModel();
                List<DeviceViewModel> devicesVM = new List<DeviceViewModel>();

                foreach (var device in trackings.Where(t => t.Location.Equals(location)).GroupBy(t => t.Device))
                {
                    devicesVM.Add(new DeviceViewModel
                    {
                        DeviceName = device.Key.Name,
                        Trackings = trackings.Where(t => t.Location.Equals(location) && t.DeviceIdDevice.Equals(device.Key.IdDevice)).OrderBy(t => t.Dtm).Select(t => new TrackingResumenViewModel
                        {
                            Dtm = t.Dtm,
                            Temperature = t.Temperature
                        }).ToList()
                    });
                }

                locationVM.LocationName = location;
                locationVM.Devices.AddRange(devicesVM);
                locations.Add(locationVM);

                return Request.CreateResponse(HttpStatusCode.OK, locations);
            }
            catch (Exception e)
            {
                var errors = ExceptionHandlerLogger.LogInnerMessages(e);
                return Request.CreateResponse(HttpStatusCode.Conflict, errors);
            }
        }

        [HttpGet]
        [Route("lastTemperature")]
        public HttpResponseMessage GetLastTemperature(string name, string family)
        {
            try
            {

                var lastTrack = dbContext.Trackings.OrderByDescending(order => order.Dtm).FirstOrDefault(t => t.Device.Name == name && t.Device.Family == family).Temperature;
                return Request.CreateResponse(HttpStatusCode.OK, lastTrack);
            }
            catch (Exception)
            {
                throw;
            }

        }

        [HttpGet]
        [Route("getDeviceInfo")]
        public HttpResponseMessage GetDeviceInfo(int deviceId)
        {

            Device device = dbContext.Devices.FirstOrDefault(d => d.IdDevice == deviceId);

            if (device != null) return Request.CreateResponse(HttpStatusCode.OK, device);

            return Request.CreateResponse(HttpStatusCode.NotFound);

        }

        /// <summary>
        /// Get Excel Report (Seguimiento de cadena de frio)
        /// </summary>
        /// <param name="id">Device id</param>
        /// <returns>excel file</returns>
        [HttpGet]
        [Route("report")]
        public HttpResponseMessage Get(int idDevice, DateTime start, DateTime end)
        {
            //2019-02-07

            if (end == null)
            {
                DateTime today = DateTime.Now.Date;
                end = new DateTime(today.Year, today.Month, DateTime.DaysInMonth(today.Year, today.Month));
                start = new DateTime(end.Year, end.Month, 1);
            }
            start = start.Date;
            end = end.Date.AddDays(1).AddTicks(-1);


            Stream templateStream = new MemoryStream();

            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "templates", "analisis.xlsx");
            using (var file = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                file.CopyTo(templateStream);
                templateStream.Seek(0, SeekOrigin.Begin);
            }

            //var templateWorkbook = new XSSFWorkbook(templateStream);
            MemoryStream memoryStream = new MemoryStream();
            var templateWorkbook = new ExcelPackage(templateStream);

            //ISheet sheet = templateWorkbook.GetSheetAt(0);
            ExcelWorksheet sheet = templateWorkbook.Workbook.Worksheets[1];
            ExcelWorksheet reporte = templateWorkbook.Workbook.Worksheets[2];
            //ISheet reporte = templateWorkbook.GetSheetAt(1);
            //FECHA     HORA        Temperatura Aceptable   Ideal
            //5/2/2018	16:15:00	4,90	    -2      8   1	4
            //c101      d101 ...
            int rowInit = 101;
            //List<Tracking> data = dbContext.Trackings.Where(t => t.Device.IdDevice == id).ToList();

            List<Tracking> centroDistribucion = dbContext.Trackings.Where(cd => cd.Location == "recepcion y carga lacteos - embutidos" || cd.Location == "recepcion carnes" || cd.Location == "despacho fruver" || cd.Location == "despacho lacteos - embutidos" && cd.DeviceIdDevice == idDevice && cd.Dtm >= start && cd.Dtm <= end).ToList();

            List<Tracking> transporte = dbContext.Trackings.Where(cd => cd.Location == "?" && cd.DeviceIdDevice == idDevice && cd.Dtm >= start && cd.Dtm <= end).ToList();

            List<Tracking> local = dbContext.Trackings.Where(cd => cd.Location == "descarga furgon" && cd.DeviceIdDevice == idDevice && cd.Dtm >= start && cd.Dtm <= end).ToList();
            List<Tracking> exibidor = dbContext.Trackings.Where(cd => cd.Location == "exibidor carnes" || cd.Location == "exibidor legumbres" || cd.Location == "exibidor pollo" || cd.Location == "exibidor lacteos" && cd.DeviceIdDevice == idDevice && cd.Dtm >= start && cd.Dtm <= end).ToList();

            if (transporte != null && transporte.Count > 0)
            {
                string loc1 = "Centro Distribución";
                string loc2 = "Transporte";
                string loc3 = "Local";
                string loc4 = "Exibidores";

                sheet.Cells[rowInit, 2].Value = loc1;
                //sheet.GetRow(rowInit).GetCell(1).SetCellValue(loc1);

                completarRegistros(sheet, reporte, centroDistribucion, rowInit, 3);

                completarRegistros(sheet, reporte, transporte, rowInit, 12);

                completarRegistros(sheet, reporte, local, rowInit, 21);

                completarRegistros(sheet, reporte, exibidor, rowInit, 30);

                sheet.Cells[rowInit, 11].Value = loc2;
                //11 fecha, 12 hora, 13 temp, 14 acep1, 15 acep2, 16 ideal1, 17 ideal2
                sheet.Cells[rowInit, 20].Value = loc3;
                //20 fecha, 21 hora, 22 temp, 23 acep1, 24 acep2, 25 ideal1, 26 ideal2
                //sheet.GetRow(rowInit).GetCell(28).SetCellValue(loc4);
                sheet.Cells[rowInit, 29].Value = loc4;
                //29 fecha, 30 hora, 31 temp, 32 acep1, 33 acep2, 34 ideal1, 35 ideal2
            }
            sheet.Cells[19, 3].Calculate();

            //XSSFFormulaEvaluator.EvaluateAllFormulaCells(templateWorkbook);
            templateWorkbook.SaveAs(memoryStream);
            templateWorkbook.Dispose();

            //templateWorkbook.Write(memoryStream);
            memoryStream.Seek(0, SeekOrigin.Begin);

            var result = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(memoryStream.ToArray())
            };
            result.Content.Headers.ContentDisposition =
                new System.Net.Http.Headers.ContentDispositionHeaderValue("attachment")
                {
                    FileName = "ReporteAnalisis.xlsx"
                };
            result.Content.Headers.ContentType =
                new MediaTypeHeaderValue("application/octet-strea");
            //application/vnd.openxmlformats-officedocument.spreadsheetml.sheet

            return result;
        }

        private void completarRegistros(ExcelWorksheet sheet, ExcelWorksheet reporte, List<Tracking> items, int rowInit, int columnInit, int reporteRowInit = 0)
        {
            for (int i = 0; i < items.Count; i++)
            {
                DateTime dtm = items[i].Dtm;
                try
                {
                    sheet.Cells[i + rowInit, columnInit].Value = $"{dtm.Date.Day}/{dtm.Date.Month}/{dtm.Date.Year}";
                    sheet.Cells[i + rowInit, columnInit + 1].Value = dtm.ToString("HH:mm:ss");
                    sheet.Cells[i + rowInit, columnInit + 2].Value = items[i].Temperature;
                    sheet.Cells[i + rowInit, columnInit + 3].Value = -2;
                    sheet.Cells[i + rowInit, columnInit + 4].Value = 8;
                    sheet.Cells[i + rowInit, columnInit + 5].Value = 1;
                    sheet.Cells[i + rowInit, columnInit + 6].Value = 4;

                    //reporte.Cells[reporteRowInit, 1].Value = i + 1;
                    //reporte.Cells[reporteRowInit, 2].Value = items[i].Location;
                    //reporte.Cells[reporteRowInit, 3].Value = $"{dtm.Date.Day}/{dtm.Date.Month}/{dtm.Date.Year}";
                    //reporte.Cells[reporteRowInit, 4].Value = dtm.ToString("HH:mm:ss");
                    //reporte.Cells[reporteRowInit, 5].Value = items[i].Temperature;
                }
                catch (Exception e)
                {
                    Request.CreateResponse(HttpStatusCode.Conflict, "Woorksheet transporte" + e.Message);
                }
                reporteRowInit++;
            }
        }
    }
}