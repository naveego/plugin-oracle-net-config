using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Google.Protobuf.Collections;
using Naveego.Sdk.Plugins;
using PluginOracleNetConfig.API.Factory;
using PluginOracleNetConfig.Helper;

namespace PluginOracleNetConfig.API.Discover
{
    public static partial class Discover
    {
        public static async IAsyncEnumerable<Schema> GetRefreshSchemas(IConnectionFactory connFactory,
            Settings settings, RepeatedField<Schema> refreshSchemas, int sampleSize = 5)
        {
            foreach (var schema in refreshSchemas)
            {
                var finalProperties = new List<Property>();
                
                if (string.IsNullOrWhiteSpace(schema.Query))
                {
                    yield return await GetRefreshSchemaForTable(connFactory, settings, schema, sampleSize);
                    continue;
                }

                // get all properties
                var schemaTable = Utility.Utility.ReadTableConfigsFromJson(settings.ConfigSchemaFilePath, schema.Id);
                var properties = schemaTable.Properties;

                if (schemaTable != null)
                {
                    var unNamedColIndex = 0;

                    // get each column and create a property for the column
                    foreach (var p in properties)
                    {
                        // get the column name
                        var pName = p.Id;
                        if (string.IsNullOrWhiteSpace(pName))
                        {
                            pName = $"UNKNOWN_{unNamedColIndex}";
                            unNamedColIndex += 1;
                        }

                        // create property
                        var property = new Property
                        {
                            Id = Utility.Utility.GetSafeName(pName),
                            Name = pName,
                            Description = "",
                            Type = GetType(p.Type),
                            TypeAtSource = p.Type,
                            IsKey = !string.IsNullOrWhiteSpace(p.IsKey.ToString()) && Boolean.Parse(p.IsKey.ToString()),
                            IsNullable = string.IsNullOrWhiteSpace(p.IsNullable.ToString()) || Boolean.Parse(p.IsNullable.ToString()),
                            IsCreateCounter = false,
                            IsUpdateCounter = false,
                            PublisherMetaJson = ""
                        };

                        // add property to properties
                        finalProperties.Add(property);
                    }
                }

                // add only discovered properties to schema
                schema.Properties.Clear();
                schema.Properties.AddRange(finalProperties);

                // get sample and count
                yield return await AddSampleAndCount(connFactory, schema, sampleSize);
            }
            
            
            
            
            // var conn = connFactory.GetConnection();
            // await conn.OpenAsync();
            //
            // foreach (var schema in refreshSchemas)
            // {
            //     if (string.IsNullOrWhiteSpace(schema.Query))
            //     {
            //         yield return await GetRefreshSchemaForTable(connFactory, schema, sampleSize);
            //         continue;
            //     }
            //
            //     var cmd = connFactory.GetCommand(schema.Query, conn);
            //
            //     var reader = await cmd.ExecuteReaderAsync();
            //     var schemaTable = reader.GetSchemaTable();
            //
            //     var properties = new List<Property>();
            //     if (schemaTable != null)
            //     {
            //         var unnamedColIndex = 0;
            //
            //          // TODO: NOTE: Is DataRow reading the columns from a schema?
            //         // get each column and create a property for the column
            //         foreach (DataRow row in schemaTable.Rows)
            //         {
            //             // get the column name
            //             var colName = row["ColumnName"].ToString();
            //             if (string.IsNullOrWhiteSpace(colName))
            //             {
            //                 colName = $"UNKNOWN_{unnamedColIndex}";
            //                 unnamedColIndex++;
            //             }
            //
            //             // create property
            //             var property = new Property
            //             {
            //                 Id = Utility.Utility.GetSafeName(colName),
            //                 Name = colName,
            //                 Description = "",
            //                 Type = GetPropertyType(row),
            //                 TypeAtSource = row["DataType"].ToString(),
            //                 IsKey = !string.IsNullOrWhiteSpace(row["IsKey"].ToString()) && Boolean.Parse(row["IsKey"].ToString()),
            //                 IsNullable = string.IsNullOrWhiteSpace(row["AllowDBNull"].ToString()) || Boolean.Parse(row["AllowDBNull"].ToString()),
            //                 IsCreateCounter = false,
            //                 IsUpdateCounter = false,
            //                 PublisherMetaJson = ""
            //             };
            //
            //             // add property to properties
            //             properties.Add(property);
            //         }
            //     }
            //
            //     // add only discovered properties to schema
            //     schema.Properties.Clear();
            //     schema.Properties.AddRange(properties);
            //
            //     // get sample and count
            //     yield return await AddSampleAndCount(connFactory, schema, sampleSize);
            // }
            //
            // await conn.CloseAsync();
        }
    }
}