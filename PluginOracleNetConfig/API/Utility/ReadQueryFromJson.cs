using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using PluginOracleNetConfig.DataContracts;

namespace PluginOracleNetConfig.API.Utility
{
    public static partial class Utility
    {
        private static List<ConfigQuery> _configQueries = new List<ConfigQuery>();
        private static string _configSchemaFilePath = "";

        public static ConfigQuery GetConfigQuery(string queryId, bool refresh = false, string filePath = "")
        {
            // load a specific schema config
            return GetConfigQueries(refresh, filePath).Find(s => s.Id.Trim() == queryId.Trim());
        }
        
        public static List<ConfigQuery> GetConfigQueries(bool refresh = false, string filePath = "")
        {
            // if refresh or first time loading
            if (refresh || _configQueries.Count == 0)
            {
                LoadQueryConfigsFromJson(_configSchemaFilePath);
            }
            
            return _configQueries;
        }

        public static void LoadQueryConfigsFromJson(string filePath)
        {
            _configSchemaFilePath = filePath;
            var duplicateIds = false;

            try
            {
                // parse json schemas from file
                var obj = JsonConvert.DeserializeObject<List<ConfigQuery>>(File.ReadAllText(filePath));

                // validate schemas before sending
                for (int i = 0; i < obj.Count; i++)
                {
                    var duplicatesCount = 1;
                    for (int i2 = 0; i2 < obj.Count; i2++)
                    {
                        if (i == i2) continue;

                        if (obj[i].Id == obj[i2].Id)
                        {
                            duplicatesCount++;
                        }
                    }

                    if (duplicatesCount > 1)
                    {
                        throw new Exception($"{duplicatesCount} queries in the configuration file contain duplicate id '{obj[i].Id}'.");
                    }
                }

                _configQueries = obj;
            }
            catch (JsonSerializationException jse)
            {
                var propertyRegex = new Regex("^Required property '([a-zA-Z_][a-zA-Z0-9_]*)'");
                var match = propertyRegex.Match(jse.Message);

                if (match.Groups.Count == 2)
                {
                    var propName = match.Groups[1].Value;
                    if (!string.IsNullOrWhiteSpace(propName))
                    {
                        if (int.TryParse(jse.Path.TrimStart('[').TrimEnd(']'), out var queryNum))
                        {
                            throw new Exception(@$"Query #{queryNum+1} in the configuration file is missing 
the property '{propName}'.".Replace("\n", ""));
                        }
                        else
                        {
                            throw new Exception(@$"A query in the configuration file is missing 
the property '{propName}'.".Replace("\n", ""));
                        }
                    }
                }
                else
                {
                    throw new Exception($"Error while parsing the config file:\n`{jse.Message}`");
                }
            }
        }
    }
}