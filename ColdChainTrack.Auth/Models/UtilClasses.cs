using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ColdChainTrack.Auth.Models
{
    public class TrackLocation
    {
        /// <summary>
        /// Timestamp
        /// </summary>
        public long t { get; set; } //timestamp
        /// <summary>
        /// Family/Group
        /// </summary>
        public string f { get; set; } //family/group
        /// <summary>
        /// Device name
        /// </summary>
        public string d { get; set; } //device
        //public string l { get; set; } //location
        /// <summary>
        /// Wifi List
        /// </summary>
        public WifiRecorded s { get; set; } // wifi list
    }

    public class WifiRecorded
    {
        /// <summary>
        /// Key value for wifi item: <mac address, intensity>
        /// </summary>
        public Dictionary<string, int> wifi = new Dictionary<string, int>(); // wifi item<mac address, intensity>
        public Dictionary<string, int> temperature = new Dictionary<string, int>(); // wifi item<mac address, intensity>
    }

    /// <summary>
    /// This class represent an response object when request Golang server
    /// </summary>
    public class GoApiResponse
    {
        public Message message { get; set; }
        public bool success { get; set; }
    }
    public class Message
    {
        public Dictionary<string, string> location_names = new Dictionary<string, string>(); // index, floor
        public List<Prediction> predictions { get; set; }
        public List<MessageGuess> guesses { get; set; }

        public string GetGuessPlace()
        {
            return (guesses != null && guesses.Count > 0) ? guesses[0].location : "";
        }
    }
    public class Prediction
    {
        public string[] locations { get; set; }
        public string name { get; set; }
        public double[] probabilities { get; set; }
    }
    public class MessageGuess
    {
        public string location { get; set; }
        public double probability { get; set; }
    }

    public partial class LocationBasicResponse
    {
        [JsonProperty("data")]
        public Data Data { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("success")]
        public bool Success { get; set; }
    }

    public partial class Data
    {
        [JsonProperty("loc")]
        public string Loc { get; set; }

        [JsonProperty("gps")]
        public Gps Gps { get; set; }

        [JsonProperty("prob")]
        public double Prob { get; set; }

        [JsonProperty("seen")]
        public long Seen { get; set; }
    }

    public partial class Gps
    {
        [JsonProperty("lat")]
        public double Lat { get; set; }

        [JsonProperty("lon")]
        public double Lon { get; set; }
    }
}