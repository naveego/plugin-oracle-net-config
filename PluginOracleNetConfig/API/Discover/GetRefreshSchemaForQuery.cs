using System;
using System.Collections.Generic;
using System.Data;
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
            var importSchema = Utility.Utility.GetConfigQuery(schema.Id, true, settings.ConfigSchemaFilePath);

            var conn = connFactory.GetConnection();

            try
            {
                await conn.OpenAsync();

                // re-create schema
                var outputSchema = new Schema()
                {
                    Id = importSchema.Id,
                    Query = "",
                    Name = importSchema.Id,
                    Description = $"Query:\n{importSchema.Query}"
                };
                
                // run the SELECT query from the import schema
                var cmd = connFactory.GetCommand(importSchema.Query, conn);
                var reader = await cmd.ExecuteReaderAsync();
                var schemaTable = reader.GetSchemaTable();

                // get the columns from the SELECT query
                outputSchema = SynthesizeSchemaFromQueryResults(outputSchema, schemaTable.Rows);
                
                return await AddSampleAndCount(connFactory, outputSchema, sampleSize);
            }
            finally
            {
                await conn.CloseAsync();
            }
        }

        private static Schema SynthesizeSchemaFromQueryResults(Schema schema, DataRowCollection rows)
        {
            var properties = new List<Property>();
                    
            var unNamedColIndex = 0;

            // get each column and create a property for the column
            foreach (DataRow row in rows)
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
                    IsKey = string.IsNullOrWhiteSpace(row["IsKey"].ToString()) ? false : Boolean.Parse(row["IsKey"].ToString()),
                    IsNullable = string.IsNullOrWhiteSpace(row["AllowDBNull"].ToString()) ? false : Boolean.Parse(row["AllowDBNull"].ToString()),
                    IsCreateCounter = false,
                    IsUpdateCounter = false,
                    PublisherMetaJson = ""
                };

                // add property to properties
                properties.Add(property);
            }

            // add only discovered properties to schema
            schema.Properties.Clear();
            schema.Properties.AddRange(properties);

            return schema;
        }
    }
}