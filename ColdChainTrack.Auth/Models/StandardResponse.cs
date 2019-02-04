using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ColdChainTrack.Auth.Models
{
    public class StandardResponse
    {
        public string Status { get; set; }
        public string Code { get; set; }
        public string Message { get; set; }

        public StandardResponse()
        {
            this.Status = "";
            this.Code = "";
            this.Message = "";
        }
        public void SetError(string Code, string Message)
        {
            this.Status = "ERROR";
            this.Code = Code;
            this.Message = Message;
        }
        public void SetOk(string Code, string Message)
        {
            this.Status = "OK";
            this.Code = Code;
            this.Message = Message;
        }
    }
}