using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
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
            var queries = JsonConvert.DeserializeObject<List<ConfigQuery>>(File.ReadAllText(filePath));
            var schemas = new List<ConfigSchema>();

            // validate queries before sending
            var unNamedSchemas = 0;
            foreach (var q in queries)
            {
                // check if id is null
                if (string.IsNullOrWhiteSpace(q.Id))
                {
                    q.Id = $"UnnamedSchema_{unNamedSchemas}";
                    unNamedSchemas += 1;
                }

                // create a new config schema object
                var currentSchema = new ConfigSchema()
                {
                    Id = q.Id,
                    Query = q.Query,
                    DataFlowDirection = "read"
                };
                
                schemas.Add(currentSchema);
            }

            return schemas;
        }
        
        public static ConfigSchema ReadSchemaConfigsFromJson(string filePath, string queryId)
        {
            // all tables in schema
            return ReadSchemaConfigsFromJson(filePath)?.Find(t => t.Id == GetTableNameFromId(queryId));
        }
    }
}