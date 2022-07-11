using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Naveego.Sdk.Plugins;
using PluginOracleNetConfig.API.Factory;
using PluginOracleNetConfig.API.Utility;
using PluginOracleNetConfig.Helper;

namespace PluginOracleNetConfig.API.Discover
{
    public static partial class Discover
    {
        public static async Task<Schema> GetRefreshSchemaForQuery(IConnectionFactory connFactory, Settings settings, Schema schema,
            int sampleSize = 5)
        {
            // read in schema from settings
            var importSchema = Utility.Utility.ReadSchemaConfigsFromJson(settings.ConfigSchemaFilePath, schema.Id);
        
            // create new schema
            var outputSchema = new Schema()
            {
                Id = importSchema.Id,
                //Query = importSchema.Query,
                Name = $"{settings.Username.ToAllCaps()}.{importSchema.Id}",
                DataFlowDirection = GetDataFlowDirection(importSchema.DataFlowDirection),
                Description = $"Query:\n{importSchema.Query}"
            };
            
            // return an updated version from the settings object
            var refreshProperties = new List<Property>();
            
            foreach (var p in importSchema.Properties)
            {
                // add column to refreshProperties
                var property = new Property
                {
                    Id = Utility.Utility.GetSafeName(p.Id),
                    Name = p.Id,
                    IsKey = p.IsKey,
                    IsNullable = p.IsNullable,
                    Type = GetType(
                        p.Type,
                        p.DataLength,
                        p.DataPrecision,
                        p.DataScale
                    ),
                    TypeAtSource = GetTypeAtSource(
                        p.Type,
                        p.DataLength,
                        p.DataPrecision,
                        p.DataScale
                    ),
                    Description = ""
                };
                
                // 
                var prevProp = refreshProperties.FirstOrDefault(p2 => p2.Id == property.Id);
                if (prevProp == null)
                {
                    refreshProperties.Add(property);
                }
                else
                {
                    var index = refreshProperties.IndexOf(prevProp);
                    refreshProperties.RemoveAt(index);
            
                    property.IsKey = prevProp.IsKey || property.IsKey;
                    refreshProperties.Add(property);
                }
            }
            
            // add properties
            outputSchema.Properties.AddRange(refreshProperties);
        
            return await AddSampleAndCount(connFactory, settings, outputSchema, sampleSize);
        }

        // private static DecomposeResponse DecomposeSafeName(string schemaId)
        // {
        //     var response = new DecomposeResponse
        //     {
        //         Database = "",
        //         Schema = "",
        //         Table = ""
        //     };
        //     var parts = schemaId.Split('.');
        //
        //     switch (parts.Length)
        //     {
        //         case 0:
        //             return response;
        //         case 1:
        //             response.Table = parts[0];
        //             return response;
        //         case 2:
        //             response.Schema = parts[0];
        //             response.Table = parts[1];
        //             return response;
        //         case 3:
        //             response.Database = parts[0];
        //             response.Schema = parts[1];
        //             response.Table = parts[2];
        //             return response;
        //         default:
        //             return response;
        //     }
        // }
        //
        // private static DecomposeResponse TrimEscape(this DecomposeResponse response, char escape = '"')
        // {
        //     response.Database = response.Database.Trim(escape);
        //     response.Schema = response.Schema.Trim(escape);
        //     response.Table = response.Table.Trim(escape);
        //
        //     return response;
        // }
    }

    // class DecomposeResponse
    // {
    //     public string Database { get; set; }
    //     public string Schema { get; set; }
    //     public string Table { get; set; }
    // }
}