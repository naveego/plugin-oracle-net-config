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
            // refresh config queries from config file
            var importSchemas = Utility.Utility.GetConfigQueries(true, settings.ConfigSchemaFilePath);
            
            var conn = connFactory.GetConnection();

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
                    
                    // re-create schema
                    var outputSchema = new Schema()
                    {
                        Id = iSchema.Id,
                        Query = "",
                        Name = iSchema.Id,
                        Description = $"Query:\n{iSchema.Query}"
                    };

                    // run the SELECT query from the import schema
                    var cmd = connFactory.GetCommand(iSchema.Query, conn);
                    var reader = await cmd.ExecuteReaderAsync();
                    var schemaTable = reader.GetSchemaTable();

                    outputSchema = SynthesizeSchemaFromQueryResults(outputSchema, schemaTable.Rows);

                    // get sample and count
                    yield return await AddSampleAndCount(connFactory, outputSchema, sampleSize);
                }
            }
            finally
            {
                await conn.CloseAsync();
            }
        }
    }
}