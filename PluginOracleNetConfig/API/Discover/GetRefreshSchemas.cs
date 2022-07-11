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
            var conn = connFactory.GetConnection();
            var importSchemas = Utility.Utility.ReadSchemaConfigsFromJson(settings.ConfigSchemaFilePath);

            try
            {
                await conn.OpenAsync();

                foreach (var schema in refreshSchemas)
                {
                    var iSchema = importSchemas.Find(s => s.Id == schema.Id);

                    if (iSchema == null)
                    {
                        yield return await GetRefreshSchemaForQuery(connFactory, settings, schema, sampleSize);
                        continue;
                    }
                    
                    // if description has no query, set schema's description to imported schema's query
                    if (string.IsNullOrWhiteSpace(schema.Description))
                    {
                        schema.Description = $"Query:\n{iSchema.Query}";
                    }

                    var cmd = connFactory.GetCommand(iSchema.Query, conn);

                    var reader = await cmd.ExecuteReaderAsync();
                    var schemaTable = reader.GetSchemaTable();

                    var properties = new List<Property>();
                    
                    if (schemaTable != null)
                    {
                        var unNamedColIndex = 0;

                        // get each column and create a property for the column
                        foreach (DataRow row in schemaTable.Rows)
                        {
                            // get the column name
                            var colName = row["ColumnName"].ToString();
                            if (string.IsNullOrWhiteSpace(colName))
                            {
                                colName = $"UNKNOWN_{unNamedColIndex}";
                                unNamedColIndex++;
                            }

                            // create property
                            var property = new Property
                            {
                                Id = Utility.Utility.GetSafeName(colName, '"'),
                                Name = colName,
                                Description = "",
                                Type = GetPropertyType(row),
                                // TypeAtSource = row["DataType"].ToString(),
                                IsKey = string.IsNullOrWhiteSpace(row["IsKey"].ToString()) ? false : Boolean.Parse(row["IsKey"].ToString()),
                                IsNullable = string.IsNullOrWhiteSpace(row["AllowDBNull"].ToString()) ? false : Boolean.Parse(row["AllowDBNull"].ToString()),
                                IsCreateCounter = false,
                                IsUpdateCounter = false,
                                PublisherMetaJson = ""
                            };

                            // add property to properties
                            properties.Add(property);
                        }
                    }

                    // add only discovered properties to schema
                    schema.Properties.Clear();
                    schema.Properties.AddRange(properties);

                    // get sample and count
                    yield return await AddSampleAndCount(connFactory, settings, schema, sampleSize);
                }
            }
            finally
            {
                await conn.CloseAsync();
            }
        }
    }
}