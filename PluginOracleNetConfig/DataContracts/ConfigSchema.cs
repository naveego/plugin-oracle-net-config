using System;
using System.Collections.Generic;
using Naveego.Sdk.Plugins;
using Newtonsoft.Json;

// Author: Gabe Hanna
// Project: Plugin Oracle .NET Config
// Created: 2022-06-10
// Modified: 2022-06-10

namespace PluginOracleNetConfig.DataContracts
{
    /// <summary>
    /// Represents a schema that is read from a configuration file
    /// </summary>
    public class ConfigSchema
    {
        [JsonProperty("id")]
        public string Id { get; set; }
        
        [JsonProperty("query")]
        public string Query { get; set; }
        
        [JsonProperty("properties")]
        public List<ConfigProperty> Properties { get; set; }

        [JsonProperty("readWriteOption")]
        public string DataFlowDirection { get; set; }

        // public ConfigSchema(string schemaId, string query, List<ConfigProperty> properties = null)
        // {
        //     Id = schemaId;
        //     Query = query;
        //
        //     _properties = new List<ConfigProperty>();
        //     
        //     if (properties != null)
        //     {
        //         foreach (var p in properties)
        //         {
        //             _properties.Add(p);
        //         }
        //     }
        // }
        //
        // public bool HasProperty(string id)
        // {
        //     // skip if these conditions are true...
        //     if (string.IsNullOrWhiteSpace(id)) return false;
        //     if (_properties == null) return false;
        //     if (_properties.Count <= 0) return false;
        //
        //     var foundMatch = false;
        //     _properties.ForEach(p =>
        //     {
        //         // skip if already found a match
        //         if (foundMatch) return;
        //         
        //         // set flag to true if found a match
        //         if (p.PropertyId == id) foundMatch = true;
        //     });
        //
        //     return foundMatch;
        // }
        //
        // public bool AddProperty(string id, string propType)
        // {
        //     // check if property exists
        //     bool exists = HasProperty(id);
        //
        //     // if not added, add to list
        //     if (!exists)
        //     {
        //         _properties.Add(new ConfigProperty
        //         {
        //             PropertyId = id,
        //             PropertyType = propType
        //         });
        //     }
        //
        //     // false if already added
        //     return !exists;
        // }
        //
        // /// <summary>
        // /// Creates a Config Schema object from a json string
        // /// </summary>
        // /// <param name="jsonStr"></param>
        // /// <returns></returns>
        // /// <exception cref="NotImplementedException"></exception>
        // public static ConfigSchema CreateFromJson(string jsonStr)
        // {
        //     throw new NotImplementedException("This method is not yet implemented.");
        // }
        //
        // /// <summary>
        // /// Creates a Config Schema object from an anonymous object
        // /// </summary>
        // /// <param name="jsonObj"></param>
        // /// <returns></returns>
        // /// <exception cref="NotImplementedException"></exception>
        // public static ConfigSchema CreateFromAnonObj(object jsonObj)
        // {
        //     throw new NotImplementedException("This method is not yet implemented.");
        // }
        //
        //
    }
    
    public class ConfigProperty
    {
        [JsonProperty("id")]
        public string Id { get; set; }
        
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("isKey")]
        public bool IsKey { get; set; }

        [JsonProperty("isNullable")]
        public bool IsNullable { get; set; } = true;
        
        [JsonProperty("dataLength")]
        public int? DataLength { get; set; }
        
        [JsonProperty("precision")]
        public int? DataPrecision { get; set; }
        
        [JsonProperty("scale")]
        public int? DataScale { get; set; }
    }
}