using System;
using System.Linq;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Naveego.Sdk.Plugins;
using PluginOracleNetConfig.API.Factory;
using PluginOracleNetConfig.API.Utility;
using PluginOracleNetConfig.DataContracts;
using PluginOracleNetConfig.Helper;

namespace PluginOracleNetConfig.API.Discover
{
    public static partial class Discover
    {
        private const string Owner = "OWNER";
        private const string TableName = "TABLE_NAME";
        private const string ColumnName = "COLUMN_NAME";
        private const string DataType = "DATA_TYPE";
        private const string DataLength = "DATA_LENGTH";
        private const string DataPrecision = "DATA_PRECISION";
        private const string DataScale = "DATA_SCALE";
        private const string Nullable = "NULLABLE";
        private const string ConstraintType = "CONSTRAINT_TYPE";

//         private const string GetAllTablesAndColumnsQuery = @"
// SELECT 
// 	t.OWNER, 
//     t.TABLE_NAME,
//     c.COLUMN_NAME,
//     c.DATA_TYPE,
//     c.DATA_LENGTH,
//     c.DATA_PRECISION,
//     c.DATA_SCALE,
//     c.NULLABLE,
//     CASE
//         WHEN tc.CONSTRAINT_TYPE = 'P'
//             THEN 'P'
//         ELSE NULL
//     END CONSTRAINT_TYPE
// FROM ALL_TABLES t
// INNER JOIN ALL_TAB_COLUMNS c ON c.OWNER = t.OWNER AND c.TABLE_NAME = t.TABLE_NAME
// LEFT OUTER JOIN all_cons_columns ccu ON ccu.COLUMN_NAME = c.COLUMN_NAME AND ccu.TABLE_NAME = t.TABLE_NAME AND ccu.OWNER = t.OWNER
// LEFT OUTER JOIN SYS.ALL_CONSTRAINTS tc ON tc.CONSTRAINT_NAME = ccu.CONSTRAINT_NAME AND tc.OWNER = ccu.OWNER
// WHERE TABLESPACE_NAME NOT IN ('SYSTEM', 'SYSAUX', 'TEMP', 'DBFS_DATA') ORDER BY t.TABLE_NAME, c.COLUMN_ID";


        public static async IAsyncEnumerable<Schema> GetAllSchemas(IConnectionFactory connFactory, Settings settings, int sampleSize = 5)
        {
            // get the config list
            List<ConfigSchema> configSchemaObject = new List<ConfigSchema>();
            List<Schema> resultSchemas = new List<Schema>();

            if (settings != null)
            {
                configSchemaObject = Utility.Utility.ReadSchemaConfigsFromJson(settings.ConfigSchemaFilePath);
            }
            
            // loop over config schemas and auto-fill properties & details
            var conn = connFactory.GetConnection();

            try
            {
                await conn.OpenAsync();

                // loop over each config list item
                foreach (var cso in configSchemaObject)
                {
                    // convert each config schema object into a schema
                    var resultSchema = new Schema()
                    {
                        Id = cso.Id,
                        //Query = $"{Utility.Utility.GetSafeName(settings.Username.ToAllCaps())}.{Utility.Utility.GetSafeName(cso.Id)}",
                        //Query = cso.Query,
                        DataFlowDirection = GetDataFlowDirection(cso.DataFlowDirection),
                        Description = "",
                        Name = cso.Id
                    };

                    // run the query attached to the schema to infer property types
                    var queryCmd = connFactory.GetCommand(cso.Query, conn);

                    var reader = await queryCmd.ExecuteReaderAsync();
                    var schemaTable = reader.GetSchemaTable();

                    if (schemaTable != null)
                    {
                        var unnamedColIndex = 0;

                        // get each column and create a property for column
                        foreach (DataRow row in schemaTable.Rows)
                        {
                            // get column name
                            var colName = row["ColumnName"].ToString();
                            if (string.IsNullOrWhiteSpace(colName))
                            {
                                colName = $"UNKNOWN_{unnamedColIndex}";
                                unnamedColIndex++;
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

                            // add property to schema
                            resultSchema.Properties.Add(property);
                        }

                        // return a schema for each config list item
                        resultSchemas.Add(resultSchema);
                    }
                }

                foreach (var rs in resultSchemas)
                {
                    if (rs != null)
                    {
                        yield return await AddSampleAndCount(connFactory, settings, rs, sampleSize);
                    }
                }

            }
            finally
            {
                await conn.CloseAsync();
            }

            // // Old Discover All method
            // var conn = connFactory.GetConnection();
            // await conn.OpenAsync();
            //
            // var customQuery = importSchema.Query;
            // var cmd = connFactory.GetCommand(customQuery, conn);
            // var reader = await cmd.ExecuteReaderAsync();
            //
            // Schema schema = null;
            // var currentSchemaId = "";
            // while (await reader.ReadAsync())
            // {
            //     var schemaId =
            //         $"{Utility.Utility.GetSafeName(reader.GetValueById(Owner).ToString())}.{Utility.Utility.GetSafeName(reader.GetValueById(TableName).ToString())}";
            //     if (schemaId != currentSchemaId)
            //     {
            //         // return previous schema
            //         if (schema != null)
            //         {
            //             // get sample and count
            //             yield return await AddSampleAndCount(connFactory, schema, sampleSize);
            //         }
            //
            //         // start new schema
            //         currentSchemaId = schemaId;
            //         var parts = DecomposeSafeName(currentSchemaId).TrimEscape();
            //         schema = new Schema
            //         {
            //             Id = currentSchemaId,
            //             Name = $"{parts.Schema}.{parts.Table}",
            //             Properties = { },
            //             DataFlowDirection = Schema.Types.DataFlowDirection.Read
            //         };
            //     }
            //
            //     // add column to schema
            //     var property = new Property
            //     {
            //         Id = Utility.Utility.GetSafeName(reader.GetValueById(ColumnName).ToString()),
            //         Name = reader.GetValueById(ColumnName).ToString(),
            //         IsKey = reader.GetValueById(ConstraintType).ToString() == "P",
            //         IsNullable = reader.GetValueById(Nullable).ToString() == "Y",
            //         Type = GetType(
            //             reader.GetValueById(DataType).ToString(),
            //             reader.GetValueById(DataLength),
            //             reader.GetValueById(DataPrecision),
            //             reader.GetValueById(DataScale)
            //         ),
            //         TypeAtSource = GetTypeAtSource(
            //             reader.GetValueById(DataType).ToString(),
            //             reader.GetValueById(DataLength),
            //             reader.GetValueById(DataPrecision),
            //             reader.GetValueById(DataScale)
            //         )
            //     };
            //
            //     var prevProp = schema?.Properties.FirstOrDefault(p => p.Id == property.Id);
            //     if (prevProp == null)
            //     {
            //         schema?.Properties.Add(property);
            //     }
            //     else
            //     {
            //         var index = schema.Properties.IndexOf(prevProp);
            //         schema.Properties.RemoveAt(index);
            //
            //         property.IsKey = prevProp.IsKey || property.IsKey;
            //         schema.Properties.Add(property);
            //     }
            // }
            //
            // await conn.CloseAsync();
            //
            // if (schema != null)
            // {
            //     // get sample and count
            //     yield return await AddSampleAndCount(connFactory, schema, sampleSize);
            // }
        }

        private static async Task<Schema> AddSampleAndCount(IConnectionFactory connFactory, Settings settings, Schema schema,
            int sampleSize)
        {
            // add sample and count
            var records = Read.Read.ReadRecords(connFactory, settings, schema).Take(sampleSize);
            schema.Sample.AddRange(await records.ToListAsync());
            schema.Count = await GetCountOfRecords(connFactory, settings, schema);

            return schema;
        }
        
        public static PropertyType GetType(string dataType, object dataLength = null, object dataPrecision = null, object dataScale = null)
        {
            switch (dataType)
            {
                case "DATE":
                case var t when t != null && t.Contains("TIMESTAMP"):
                    return PropertyType.Datetime;
                case "TIME":
                    return PropertyType.Time;
                case "NUMBER":
                    if (dataScale != null && dataPrecision != null)
                    {
                        if ((int)dataScale == 0 || (int)dataScale == -127)
                        {
                            if ((int)dataPrecision <= 16)
                            {
                                return PropertyType.Integer;
                            }
                        }
                    }

                    return PropertyType.Decimal;
                case "FLOAT":
                case "BINARY_FLOAT":
                case "DOUBLE":
                case "BINARY_DOUBLE":
                    return PropertyType.Float;
                case "DECIMAL":
                case "BIGINT":
                    return PropertyType.Decimal;
                case "BOOLEAN":
                    return PropertyType.Bool;
                case "BLOB":
                    return PropertyType.Blob;
                case "XMLTYPE":
                    return PropertyType.Xml;
                case "CLOB":
                case "NCLOB":
                    return PropertyType.Text;
                case "CHAR":
                case "VARCHAR":
                case "NCHAR":
                case "NVARCHAR":
                case "VARCHAR2":
                case "NVARCHAR2":
                    if (dataLength != null)
                    {
                        if ((int)dataLength >= 1024)
                        {
                            return PropertyType.Text;
                        }
                    }
                    return PropertyType.String;
                default:
                    return PropertyType.String;
            }
        }

        // public static PropertyType GetTypeFromSchema(string dataType)
        // {
        //     switch (dataType.ToLower())
        //     {
        //         case "datetime":
        //             return PropertyType.Datetime;
        //         case "time":
        //             return PropertyType.Time;
        //         case "integer":
        //             return PropertyType.Integer;
        //         case "decimal":
        //             return PropertyType.Decimal;
        //         case "float":
        //             return PropertyType.Float;
        //         case "bool":
        //             return PropertyType.Bool;
        //         case "blob":
        //             return PropertyType.Blob;
        //         case "xml":
        //             return PropertyType.Xml;
        //         case "text":
        //             return PropertyType.Text;
        //         default:
        //             return PropertyType.String;
        //     }
        // }
        
        private static string GetTypeAtSource(string dataType, object dataLength, object dataPrecision, object dataScale)
        {
            switch (dataType)
            {
                case "CHAR":
                case "VARCHAR2":
                case "NCHAR":
                case "NVARCHAR2":
                    if (dataLength != DBNull.Value)
                    {
                        return $"{dataType}({dataLength})";
                    }
                    break;
                case "NUMBER":
                    if (dataPrecision != DBNull.Value && dataScale != DBNull.Value)
                    {
                        return $"{dataType}({dataPrecision},{dataScale})";
                    }
                    break;
            }

            return dataType;
        }

        public static Schema.Types.DataFlowDirection GetDataFlowDirection(string direction)
        {
            switch (direction)
            {
                case var t when t.Contains("write") && t.Contains("read"):
                    return Schema.Types.DataFlowDirection.ReadWrite;
                case var t when t.Contains("write"):
                    return Schema.Types.DataFlowDirection.Write;
                default:
                    return Schema.Types.DataFlowDirection.Read;
            }
        }
    }
}