using AutoMapper;
using ColdChainTrack.Auth.Models;
using ColdChainTrack.Auth.Utils;
using Newtonsoft.Json;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
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
        public HttpResponseMessage GetAlltrackingGroupBy()
        {
            try
            {
                var currentTrackings = dbContext.Trackings.GroupBy(x => new
                {
                    x.Location,
                    //x.Dtm,
                    //x.Temperature,
                    //x.Latitude,
                    //x.Longitude,
                    //x.Device.Name,
                    //x.Device.Family
                }).ToList();
                //.Select(g => new { g.Key.Location, g.Key.Dtm, g.Key.Temperature, g.Key.Latitude, g.Key.Longitude, g.Key.Name, g.Key.Family })
                //.OrderBy(o => o.Dtm).ToList();

                return Request.CreateResponse(HttpStatusCode.OK, currentTrackings);
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

        /// <summary>
        /// Get Excel Report (Seguimiento de cadena de frio)
        /// </summary>
        /// <param name="id">Device id</param>
        /// <returns>excel file</returns>
        [HttpGet]
        [Route("report")]
        public HttpResponseMessage Get(int id)
        {
            Stream templateStream = new MemoryStream();

            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "templates", "analisis.xlsx");
            using (var file = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                file.CopyTo(templateStream);
                templateStream.Seek(0, SeekOrigin.Begin);
            }
            var templateWorkbook = new XSSFWorkbook(templateStream);
            ISheet sheet = templateWorkbook.GetSheetAt(0);
            ISheet reporte = templateWorkbook.GetSheetAt(1);
            //FECHA     HORA        Temperatura Aceptable   Ideal
            //5/2/2018	16:15:00	4,90	    -2      8   1	4
            //c101      d101 ...

            Random rdn = new Random();
            int rowInit = 100;
            int reporteRowInit = 26;
            //List<Tracking> data = dbContext.Trackings.Where(t => t.Device.IdDevice == id).ToList();

            List<Tracking> centroDistribucion = dbContext.Trackings.Where(cd => cd.Location == "recepcion y carga lacteos - embutidos" || cd.Location == "recepcion carnes" || cd.Location == "despacho fruver" || cd.Location == "despacho lacteos - embutidos").ToList();

            List<Tracking> transporte = dbContext.Trackings.Where(cd => cd.Location == "?").ToList();

            List<Tracking> local = dbContext.Trackings.Where(cd => cd.Location == "descarga furgon").ToList();
            List<Tracking> exibidor = dbContext.Trackings.Where(cd => cd.Location == "exibidor carnes" || cd.Location == "exibidor legumbres" || cd.Location == "exibidor pollo" || cd.Location == "exibidor lacteos").ToList();

            if (centroDistribucion != null && centroDistribucion.Count > 0)
            {

                //var info = getLocations(data);
                string loc1 = "Centro Distribución";
                string loc2 = "Transporte";
                string loc3 = "Local";
                string loc4 = "Exibidores";

                sheet.GetRow(rowInit).GetCell(1).SetCellValue(loc1);


                for (int i = 0; i < exibidor.Count; i++)
                {
                    DateTime dtm = exibidor[i].Dtm;
                    try
                    {
                        //sheet.GetRow(i + rowInit).GetCell(29).SetCellValue($"{dtm.Date.Day}/{dtm.Date.Month}/{dtm.Date.Year}");
                        //sheet.GetRow(i + rowInit).GetCell(31).SetCellValue(dtm.ToString("HH:mm:ss"));
                        //sheet.GetRow(i + rowInit).GetCell(32).SetCellValue(exibidor[i].Temperature);
                        //sheet.GetRow(i + rowInit).GetCell(33).SetCellValue(-2);
                        //sheet.GetRow(i + rowInit).GetCell(34).SetCellValue(8);
                        //sheet.GetRow(i + rowInit).GetCell(35).SetCellValue(1);
                        //sheet.GetRow(i + rowInit).GetCell(36).SetCellValue(4);

                        //reporte.GetRow(reporteRowInit).GetCell(0).SetCellValue(i + 1);
                        //reporte.GetRow(reporteRowInit).GetCell(1).SetCellValue(exibidor[i].Location);
                        //reporte.GetRow(reporteRowInit).GetCell(2).SetCellValue($"{dtm.Date.Day}/{dtm.Date.Month}/{dtm.Date.Year}");

                        //reporte.GetRow(reporteRowInit).GetCell(3).SetCellValue(dtm.ToString("HH:mm:ss"));
                        //reporte.GetRow(reporteRowInit).GetCell(4).SetCellValue(exibidor[i].Temperature);
                    }
                    catch (Exception e)
                    {
                        //IRow _row = sheet.CreateRow(i + rowInit);
                        //ICell _cell = _row.CreateCell(29);
                        //_cell.SetCellValue($"{dtm.Date.Day}/{dtm.Date.Month}/{dtm.Date.Year}");
                        //ICell _cell2 = _row.CreateCell(30);
                        //_cell2.SetCellValue(dtm.ToString("HH:mm:ss"));
                        //ICell _cell3 = _row.CreateCell(31);
                        //_cell3.SetCellValue(exibidor[i].Temperature);
                        //ICell _cell4 = _row.CreateCell(32);
                        //_cell4.SetCellValue(-2);
                        //ICell _cell5 = _row.CreateCell(33);
                        //_cell5.SetCellValue(8);
                        //ICell _cell6 = _row.CreateCell(34);
                        //_cell6.SetCellValue(1);
                        //ICell _cell7 = _row.CreateCell(35);
                        //_cell7.SetCellValue(4);

                        //IRow _row2 = reporte.CreateRow(reporteRowInit);
                        //ICell _cellR = _row2.CreateCell(0);
                        //_cellR.SetCellValue(i + 1);
                        //ICell _cellR2 = _row2.CreateCell(1);
                        //_cellR2.SetCellValue(exibidor[i].Location);
                        //ICell _cellR3 = _row2.CreateCell(2);
                        //_cellR3.SetCellValue($"{dtm.Date.Day}/{dtm.Date.Month}/{dtm.Date.Year}");
                        //ICell _cellR4 = _row2.CreateCell(3);
                        //_cellR4.SetCellValue(dtm.ToString("HH:mm:ss"));
                        //ICell _cellR5 = _row2.CreateCell(4);
                        //_cellR5.SetCellValue(exibidor[i].Temperature);
                    }
                    reporteRowInit++;
                }

                //    /*
                //    //sheet.GetRow(i + rowInit).GetCell(4).SetCellValue(data[i].Temperature);
                //    createCell(sheet, i + rowInit, 4, "", data[i].Temperature);
                //    //sheet.GetRow(i + rowInit).GetCell(5).SetCellValue(-2);
                //    createCell(sheet, i + rowInit, 5, "", -2);
                //    //sheet.GetRow(i + rowInit).GetCell(6).SetCellValue(8);
                //    createCell(sheet, i + rowInit, 6, "", 8);
                //    //sheet.GetRow(i + rowInit).GetCell(7).SetCellValue(1);
                //    createCell(sheet, i + rowInit, 7, "", 1);
                //    //sheet.GetRow(i + rowInit).GetCell(8).SetCellValue(4);
                //    createCell(sheet, i + rowInit, 8, "", 4);

                //    //reporte.GetRow(i + reporteRowInit).GetCell(0).SetCellValue(i + 1);
                //    createCell(reporte, i + reporteRowInit, 0, "", i+1);
                //    //reporte.GetRow(i + reporteRowInit).GetCell(1).SetCellValue(data[i].Location);
                //    createCell(reporte, i + reporteRowInit, 1, data[i].Location);
                //    //reporte.GetRow(i + reporteRowInit).GetCell(2).SetCellValue($"{dtm.Date.Day}/{dtm.Date.Month}/{dtm.Date.Year}");
                //    createCell(reporte, i + reporteRowInit, 2, $"{dtm.Date.Day}/{dtm.Date.Month}/{dtm.Date.Year}");
                //    //reporte.GetRow(i + reporteRowInit).GetCell(3).SetCellValue(dtm.ToString("HH:mm:ss"));
                //    createCell(reporte, i + reporteRowInit, 3, dtm.ToString("HH:mm:ss"));
                //    //reporte.GetRow(i + reporteRowInit).GetCell(4).SetCellValue(data[i].Temperature);
                //    createCell(reporte, i + reporteRowInit, 4, "", data[i].Temperature);
                //    */


                //    /*
                //    //Piso 2
                //    //11 fecha, 12 hora, 13 temp, 14 acep1, 15 acep2, 16 ideal1, 17 ideal2

                //    sheet.GetRow(i + rowInit).GetCell(11).SetCellValue($"{dtm.Date.Day}/{dtm.Date.Month}/{dtm.Date.Year}");

                //    sheet.GetRow(i + rowInit).GetCell(12).SetCellValue(dtm.ToString("HH:mm:ss"));
                //    sheet.GetRow(i + rowInit).GetCell(13).SetCellValue(Math.Round((rdn.NextDouble() * 10 + 1), 2));
                //    sheet.GetRow(i + rowInit).GetCell(14).SetCellValue(-2);
                //    sheet.GetRow(i + rowInit).GetCell(15).SetCellValue(8);
                //    sheet.GetRow(i + rowInit).GetCell(16).SetCellValue(1);
                //    sheet.GetRow(i + rowInit).GetCell(17).SetCellValue(4);

                //    //Piso 3
                //    //20 fecha, 21 hora, 22 temp, 23 acep1, 24 acep2, 25 ideal1, 26 ideal2

                //    sheet.GetRow(i + rowInit).GetCell(20).SetCellValue($"{dtm.Date.Day}/{dtm.Date.Month}/{dtm.Date.Year}");
                //    sheet.GetRow(i + rowInit).GetCell(21).SetCellValue(dtm.ToString("HH:mm:ss"));
                //    sheet.GetRow(i + rowInit).GetCell(22).SetCellValue(Math.Round((rdn.NextDouble() * 10 + 1), 2));
                //    sheet.GetRow(i + rowInit).GetCell(23).SetCellValue(-2);
                //    sheet.GetRow(i + rowInit).GetCell(24).SetCellValue(8);
                //    sheet.GetRow(i + rowInit).GetCell(25).SetCellValue(1);
                //    sheet.GetRow(i + rowInit).GetCell(26).SetCellValue(4);

                //    //Piso 4
                //    //29 fecha, 30 hora, 31 temp, 32 acep1, 33 acep2, 34 ideal1, 35 ideal2

                //    sheet.GetRow(i + rowInit).GetCell(29).SetCellValue($"{dtm.Date.Day}/{dtm.Date.Month}/{dtm.Date.Year}");
                //    sheet.GetRow(i + rowInit).GetCell(30).SetCellValue(dtm.ToString("HH:mm:ss"));
                //    sheet.GetRow(i + rowInit).GetCell(31).SetCellValue(Math.Round((rdn.NextDouble() * 10 + 1), 2));
                //    sheet.GetRow(i + rowInit).GetCell(32).SetCellValue(-2);
                //    sheet.GetRow(i + rowInit).GetCell(33).SetCellValue(8);
                //    sheet.GetRow(i + rowInit).GetCell(34).SetCellValue(1);
                //    sheet.GetRow(i + rowInit).GetCell(35).SetCellValue(4);
                //    */
                //    reporteRowInit++;
                //}

                //for (int i = 0; i < transporte.Count; i++)
                //{
                //    DateTime dtm = transporte[i].Dtm;
                //    try
                //    {
                //        sheet.GetRow(i + rowInit).GetCell(20).SetCellValue($"{dtm.Date.Day}/{dtm.Date.Month}/{dtm.Date.Year}");
                //        sheet.GetRow(i + rowInit).GetCell(21).SetCellValue(dtm.ToString("HH:mm:ss"));
                //        sheet.GetRow(i + rowInit).GetCell(22).SetCellValue(transporte[i].Temperature);
                //        sheet.GetRow(i + rowInit).GetCell(23).SetCellValue(-2);
                //        sheet.GetRow(i + rowInit).GetCell(24).SetCellValue(8);
                //        sheet.GetRow(i + rowInit).GetCell(25).SetCellValue(1);
                //        sheet.GetRow(i + rowInit).GetCell(26).SetCellValue(4);

                //        reporte.GetRow(reporteRowInit).GetCell(0).SetCellValue(i + 1);
                //        reporte.GetRow(reporteRowInit).GetCell(1).SetCellValue(transporte[i].Location);
                //        reporte.GetRow(reporteRowInit).GetCell(2).SetCellValue($"{dtm.Date.Day}/{dtm.Date.Month}/{dtm.Date.Year}");

                //        reporte.GetRow(reporteRowInit).GetCell(3).SetCellValue(dtm.ToString("HH:mm:ss"));
                //        reporte.GetRow(reporteRowInit).GetCell(4).SetCellValue(transporte[i].Temperature);
                //    }
                //    catch
                //    {
                //        IRow _row = sheet.CreateRow(i + rowInit);
                //        ICell _cell = _row.CreateCell(20);
                //        _cell.SetCellValue($"{dtm.Date.Day}/{dtm.Date.Month}/{dtm.Date.Year}");
                //        ICell _cell2 = _row.CreateCell(21);
                //        _cell2.SetCellValue(dtm.ToString("HH:mm:ss"));
                //        ICell _cell3 = _row.CreateCell(22);
                //        _cell3.SetCellValue(transporte[i].Temperature);
                //        ICell _cell4 = _row.CreateCell(23);
                //        _cell4.SetCellValue(-2);
                //        ICell _cell5 = _row.CreateCell(24);
                //        _cell5.SetCellValue(8);
                //        ICell _cell6 = _row.CreateCell(25);
                //        _cell6.SetCellValue(1);
                //        ICell _cell7 = _row.CreateCell(26);
                //        _cell7.SetCellValue(4);

                //        IRow _row2 = reporte.CreateRow(reporteRowInit);
                //        ICell _cellR = _row2.CreateCell(0);
                //        _cellR.SetCellValue(i + 1);
                //        ICell _cellR2 = _row2.CreateCell(1);
                //        _cellR2.SetCellValue(transporte[i].Location);
                //        ICell _cellR3 = _row2.CreateCell(2);
                //        _cellR3.SetCellValue($"{dtm.Date.Day}/{dtm.Date.Month}/{dtm.Date.Year}");
                //        ICell _cellR4 = _row2.CreateCell(3);
                //        _cellR4.SetCellValue(dtm.ToString("HH:mm:ss"));
                //        ICell _cellR5 = _row2.CreateCell(4);
                //        _cellR5.SetCellValue(transporte[i].Temperature);
                //    }
                //    reporteRowInit++;
                //}


                //for (int i = 0; i < local.Count; i++)
                //{
                //    DateTime dtm = local[i].Dtm;
                //    try
                //    {
                //        sheet.GetRow(i + rowInit).GetCell(20).SetCellValue($"{dtm.Date.Day}/{dtm.Date.Month}/{dtm.Date.Year}");
                //        sheet.GetRow(i + rowInit).GetCell(21).SetCellValue(dtm.ToString("HH:mm:ss"));
                //        sheet.GetRow(i + rowInit).GetCell(22).SetCellValue(local[i].Temperature);
                //        sheet.GetRow(i + rowInit).GetCell(23).SetCellValue(-2);
                //        sheet.GetRow(i + rowInit).GetCell(24).SetCellValue(8);
                //        sheet.GetRow(i + rowInit).GetCell(25).SetCellValue(1);
                //        sheet.GetRow(i + rowInit).GetCell(26).SetCellValue(4);

                //        reporte.GetRow(reporteRowInit).GetCell(0).SetCellValue(i + 1);
                //        reporte.GetRow(reporteRowInit).GetCell(1).SetCellValue(local[i].Location);
                //        reporte.GetRow(reporteRowInit).GetCell(2).SetCellValue($"{dtm.Date.Day}/{dtm.Date.Month}/{dtm.Date.Year}");

                //        reporte.GetRow(reporteRowInit).GetCell(3).SetCellValue(dtm.ToString("HH:mm:ss"));
                //        reporte.GetRow(reporteRowInit).GetCell(4).SetCellValue(local[i].Temperature);
                //    }
                //    catch
                //    {
                //        IRow _row = sheet.CreateRow(i + rowInit);
                //        ICell _cell = _row.CreateCell(20);
                //        _cell.SetCellValue($"{dtm.Date.Day}/{dtm.Date.Month}/{dtm.Date.Year}");
                //        ICell _cell2 = _row.CreateCell(21);
                //        _cell2.SetCellValue(dtm.ToString("HH:mm:ss"));
                //        ICell _cell3 = _row.CreateCell(22);
                //        _cell3.SetCellValue(local[i].Temperature);
                //        ICell _cell4 = _row.CreateCell(23);
                //        _cell4.SetCellValue(-2);
                //        ICell _cell5 = _row.CreateCell(24);
                //        _cell5.SetCellValue(8);
                //        ICell _cell6 = _row.CreateCell(25);
                //        _cell6.SetCellValue(1);
                //        ICell _cell7 = _row.CreateCell(26);
                //        _cell7.SetCellValue(4);

                //        IRow _row2 = reporte.CreateRow(reporteRowInit);
                //        ICell _cellR = _row2.CreateCell(0);
                //        _cellR.SetCellValue(i + 1);
                //        ICell _cellR2 = _row2.CreateCell(1);
                //        _cellR2.SetCellValue(local[i].Location);
                //        ICell _cellR3 = _row2.CreateCell(2);
                //        _cellR3.SetCellValue($"{dtm.Date.Day}/{dtm.Date.Month}/{dtm.Date.Year}");
                //        ICell _cellR4 = _row2.CreateCell(3);
                //        _cellR4.SetCellValue(dtm.ToString("HH:mm:ss"));
                //        ICell _cellR5 = _row2.CreateCell(4);
                //        _cellR5.SetCellValue(transporte[i].Temperature);
                //    }
                //    reporteRowInit++;
                //}

                //for (int i = 0; i < exibidor.Count; i++)
                //{
                //    DateTime dtm = exibidor[i].Dtm;
                //    try
                //    {
                //        sheet.GetRow(i + rowInit).GetCell(29).SetCellValue($"{dtm.Date.Day}/{dtm.Date.Month}/{dtm.Date.Year}");
                //        sheet.GetRow(i + rowInit).GetCell(31).SetCellValue(dtm.ToString("HH:mm:ss"));
                //        sheet.GetRow(i + rowInit).GetCell(32).SetCellValue(exibidor[i].Temperature);
                //        sheet.GetRow(i + rowInit).GetCell(33).SetCellValue(-2);
                //        sheet.GetRow(i + rowInit).GetCell(34).SetCellValue(8);
                //        sheet.GetRow(i + rowInit).GetCell(35).SetCellValue(1);
                //        sheet.GetRow(i + rowInit).GetCell(36).SetCellValue(4);

                //        reporte.GetRow(reporteRowInit).GetCell(0).SetCellValue(i + 1);
                //        reporte.GetRow(reporteRowInit).GetCell(1).SetCellValue(exibidor[i].Location);
                //        reporte.GetRow(reporteRowInit).GetCell(2).SetCellValue($"{dtm.Date.Day}/{dtm.Date.Month}/{dtm.Date.Year}");

                //        reporte.GetRow(reporteRowInit).GetCell(3).SetCellValue(dtm.ToString("HH:mm:ss"));
                //        reporte.GetRow(reporteRowInit).GetCell(4).SetCellValue(exibidor[i].Temperature);
                //    }
                //    catch (Exception e)
                //    {
                //        IRow _row = sheet.CreateRow(i + rowInit);
                //        ICell _cell = _row.CreateCell(29);
                //        _cell.SetCellValue($"{dtm.Date.Day}/{dtm.Date.Month}/{dtm.Date.Year}");
                //        ICell _cell2 = _row.CreateCell(30);
                //        _cell2.SetCellValue(dtm.ToString("HH:mm:ss"));
                //        ICell _cell3 = _row.CreateCell(31);
                //        _cell3.SetCellValue(exibidor[i].Temperature);
                //        ICell _cell4 = _row.CreateCell(32);
                //        _cell4.SetCellValue(-2);
                //        ICell _cell5 = _row.CreateCell(33);
                //        _cell5.SetCellValue(8);
                //        ICell _cell6 = _row.CreateCell(34);
                //        _cell6.SetCellValue(1);
                //        ICell _cell7 = _row.CreateCell(35);
                //        _cell7.SetCellValue(4);

                //        IRow _row2 = reporte.CreateRow(reporteRowInit);
                //        ICell _cellR = _row2.CreateCell(0);
                //        _cellR.SetCellValue(i + 1);
                //        ICell _cellR2 = _row2.CreateCell(1);
                //        _cellR2.SetCellValue(exibidor[i].Location);
                //        ICell _cellR3 = _row2.CreateCell(2);
                //        _cellR3.SetCellValue($"{dtm.Date.Day}/{dtm.Date.Month}/{dtm.Date.Year}");
                //        ICell _cellR4 = _row2.CreateCell(3);
                //        _cellR4.SetCellValue(dtm.ToString("HH:mm:ss"));
                //        ICell _cellR5 = _row2.CreateCell(4);
                //        _cellR5.SetCellValue(exibidor[i].Temperature);
                //    }
                //    reporteRowInit++;
                //}

                sheet.GetRow(rowInit).GetCell(10).SetCellValue(loc2);
                //11 fecha, 12 hora, 13 temp, 14 acep1, 15 acep2, 16 ideal1, 17 ideal2
                sheet.GetRow(rowInit).GetCell(19).SetCellValue(loc3);
                //20 fecha, 21 hora, 22 temp, 23 acep1, 24 acep2, 25 ideal1, 26 ideal2
                sheet.GetRow(rowInit).GetCell(28).SetCellValue(loc4);
                //29 fecha, 30 hora, 31 temp, 32 acep1, 33 acep2, 34 ideal1, 35 ideal2
            }

            XSSFFormulaEvaluator.EvaluateAllFormulaCells(templateWorkbook);

            MemoryStream memoryStream = new MemoryStream();
            templateWorkbook.Write(memoryStream);
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

        private IEnumerable<LocationName> getLocations(List<Tracking> data)
        {
            var locations = data.GroupBy(d => d.Location, (key, group) => new LocationName { name = key });

            return locations;
        }


    }
}