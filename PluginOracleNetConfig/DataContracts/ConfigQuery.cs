using Newtonsoft.Json;

// Author: Gabe Hanna
// Project: Plugin Oracle .NET Config
// Created: 2022-06-10
// Modified: 2022-06-10

namespace PluginOracleNetConfig.DataContracts
{
    /// <summary>
    /// Represents a named query that is read from a configuration file
    /// </summary>
    public class ConfigQuery
    {
        [JsonProperty("id", Required = Required.Always)]
        public string Id { get; set; }
        
        [JsonProperty("query", Required = Required.Always)]
        public string Query { get; set; }
    }
}