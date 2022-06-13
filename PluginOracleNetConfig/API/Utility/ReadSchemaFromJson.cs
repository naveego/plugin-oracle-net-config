using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using Newtonsoft.Json;
using PluginOracleNetConfig.DataContracts;
using Serilog.Core;

namespace PluginOracleNetConfig.API.Utility
{
    public static partial class Utility
    {
        public static List<ConfigSchema> ReadSchemaConfigsFromJson(string filePath)
        {
            // parse json schemas from file
            var obj = JsonConvert.DeserializeObject<List<ConfigSchema>>(File.ReadAllText(filePath));
            
            // validate schemas before sending
            var unNamedSchemas = 0;
            foreach (var configSchema in obj)
            {
                // check if id is null
                if (string.IsNullOrWhiteSpace(configSchema.Id))
                {
                    configSchema.Id = $"UnnamedSchema_{unNamedSchemas}";
                    unNamedSchemas += 1;
                }

                // TODO: validate properties by type
                
            }

            return obj;
        }
        
        public static ConfigSchema ReadTableConfigsFromJson(string filePath, string tableId)
        {
            // all tables in schema
            var configSchema = ReadSchemaConfigsFromJson(filePath);

            var table = configSchema?.Find(t => t.Id == GetTableNameFromId(tableId));

            // if (table == null)
            // {
            //     // TODO: Possibly throw error here if table not found
            //     return null;
            // }

            return table;
        }
    }
}