﻿namespace DaybreakGames.Census
{
    public class CensusOptions
    {
        public string CensusServiceId { get; set; } = Constants.DefaultServiceId;
        public string CensusServiceNamespace { get; set; } = Constants.DefaultServiceNamespace;
        public string CensusApiEndpoint { get; set; } = Constants.CensusEndpoint;
        public string CensusWebsocketEndpoint { get; set; } = Constants.CensusWebsocketEndpoint;
        public string UserAgent { get; set; } = null;
        public bool UseHttps { get; set; } = false;
        public bool LogCensusErrors { get; set; } = false;
    }
}
