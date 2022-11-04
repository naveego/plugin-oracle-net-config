using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using PluginOracleNetConfig.Helper;
using PluginOracleNetConfig.DataContracts;
using Newtonsoft.Json;
using Naveego.Sdk.Plugins;
using Xunit;
using System.Threading.Tasks;
using Grpc.Core;
using System.Linq;
using Record = Naveego.Sdk.Plugins.Record;

namespace PluginOracleNetTest.Plugin
{
    public class PluginIntegrationTest
    {
        // Test Variables

        private static string TestSchemaID = "Query1";
        private static string TestSchemaName = "Query1";
        private static string TestSchemaQuery = "SELECT ID, LAST_NAME, FIRST_NAME FROM \"<schema_name>\".\"<table_name>\"";
        private static string TestSchemaQuery2 = "SELECT ID, LAST_NAME, FIRST_NAME, EMAIL FROM \"<schema_name>\".\"<table_name>\"";
        private static int TestSampleCount = 10;
        private static string TestPropertyID = "\"ID\"";
        private static string TestPropertyName = "ID";
        private static int TestPropertyCount = 3;

        private static string TestPropertyID2 = "\"EMAIL\"";
        private static string TestPropertyName2 = "EMAIL";
        private static int TestPropertyCount2 = 4;

        // (When testing) Specify path to config file folder
        private static string TestConfigPrefix = "/fill/me/in/during/testing";
        private static string TestConfigSchemaFilePath = $"{TestConfigPrefix}-current.json";
        private static string PropsMissingConfigSchemaFilePath = $"{TestConfigPrefix}.json";
        private static string DuplicateIdsConfigSchemaFilePath = $"{TestConfigPrefix}2.json";
        private static string AltConfigSchemaFilePath = $"{TestConfigPrefix}4.json";
        private static string RestoreConfigSchemaFilePath = $"{TestConfigPrefix}3.json";
        
        // Settings fields
        private const string SettingsHostname = "";
        private const string SettingsPort = "";
        private const string SettingsPassword = "";
        private const string SettingsUsername = "";
        private const string SettingsServiceName = "";

        private Settings GetSettings()
        {
            return new Settings
            {
                Hostname = SettingsHostname,
                Port = SettingsPort,
                Password = SettingsPassword,
                Username = SettingsUsername,
                ServiceName = SettingsServiceName,
                ConfigSchemaFilePath = TestConfigSchemaFilePath
            };
        }

        private ConnectRequest GetConnectSettings()
        {
            var settings = GetSettings();

            return new ConnectRequest
            {
                SettingsJson = JsonConvert.SerializeObject(settings),
                OauthConfiguration = new OAuthConfiguration(),
                OauthStateJson = ""
            };
        }

        private Schema GetTestSchema(string id = "test", string name = "test", string query = "")
        {
            return new Schema
            {
                Id = id,
                Name = name,
                Query = query,
                Properties =
                {
                    new Property
                    {
                        Id = "Id",
                        Name = "Id",
                        Type = PropertyType.Integer,
                        IsKey = true
                    },
                    new Property
                    {
                        Id = "Name",
                        Name = "Name",
                        Type = PropertyType.String
                    }
                }
            };
        }

        private Schema GetTestReplicationSchema(string id = "test", string name = "test", string query = "")
        {
            // --- Note: Changed to fit the schema of the <table_name> table ---
            // Change query if empty
            if (string.IsNullOrWhiteSpace(query))
            {
                query = TestSchemaID;
            }
            
            // build schema
            return new Schema
            {
                Id = id,
                Name = name,
                Query = query,
                Properties =
                {
                    new Property
                    {
                        Id = "ID",
                        Name = "ID",
                        Type = PropertyType.String,
                        IsKey = false
                    },
                    new Property
                    {
                        Id = "FIRST_NAME",
                        Name = "FIRST_NAME",
                        Type = PropertyType.String,
                        IsKey = false
                    },
                    new Property
                    {
                        Id = "LAST_NAME",
                        Name = "LAST_NAME",
                        Type = PropertyType.String,
                        IsKey = false
                    }
                    ,
                    new Property
                    {
                        Id = "EMAIL",
                        Name = "EMAIL",
                        Type = PropertyType.String,
                        IsKey = false
                    },
                    new Property
                    {
                        Id = "ADDRESS",
                        Name = "ADDRESS",
                        Type = PropertyType.String,
                        IsKey = false
                    },
                    new Property
                    {
                        Id = "CITY",
                        Name = "CITY",
                        Type = PropertyType.String,
                        IsKey = false
                    },
                    new Property
                    {
                        Id = "STATE",
                        Name = "STATE",
                        Type = PropertyType.String,
                        IsKey = false
                    },
                    new Property
                    {
                        Id = "ZIP",
                        Name = "ZIP",
                        Type = PropertyType.Integer,
                        IsKey = false
                    },
                    new Property
                    {
                        Id = "GENERAL_LEDGER",
                        Name = "GENERAL_LEDGER",
                        Type = PropertyType.Integer,
                        IsKey = false
                    },
                    new Property
                    {
                        Id = "BALANCE",
                        Name = "BALANCE",
                        Type = PropertyType.Decimal,
                        IsKey = false
                    },
                    new Property
                    {
                        Id = "REP",
                        Name = "REP",
                        Type = PropertyType.String,
                        IsKey = false
                    }
                }
            };
        }

        private void UpdateConfigFile(bool alternative = false)
        {
            var restoreContents = File.ReadAllText(alternative ? AltConfigSchemaFilePath : RestoreConfigSchemaFilePath);
            File.WriteAllText(TestConfigSchemaFilePath, restoreContents);
        }
        
        [Fact]
        public async Task ConnectSessionTest()
        {
            // setup
            Server server = new Server
            {
                Services = { Publisher.BindService(new PluginOracleNetConfig.Plugin.Plugin()) },
                Ports = { new ServerPort("localhost", 0, ServerCredentials.Insecure) }
            };
            server.Start();

            var port = server.Ports.First().BoundPort;

            var channel = new Channel($"localhost:{port}", ChannelCredentials.Insecure);
            var client = new Publisher.PublisherClient(channel);

            var request = GetConnectSettings();
            var disconnectRequest = new DisconnectRequest();

            // act
            var response = client.ConnectSession(request);
            var responseStream = response.ResponseStream;
            var records = new List<ConnectResponse>();

            while (await responseStream.MoveNext())
            {
                records.Add(responseStream.Current);
                client.Disconnect(disconnectRequest);
            }

            // assert
            Assert.Single(records);

            // cleanup
            await channel.ShutdownAsync();
            await server.ShutdownAsync();
        }

        [Fact]
        public async Task ConnectTest()
        {
            // setup
            Server server = new Server
            {
                Services = { Publisher.BindService(new PluginOracleNetConfig.Plugin.Plugin()) },
                Ports = { new ServerPort("localhost", 0, ServerCredentials.Insecure) }
            };
            server.Start();

            var port = server.Ports.First().BoundPort;

            var channel = new Channel($"localhost:{port}", ChannelCredentials.Insecure);
            var client = new Publisher.PublisherClient(channel);

            var request = GetConnectSettings();

            // act
            var response = client.Connect(request);

            // assert
            Assert.IsType<ConnectResponse>(response);
            Assert.Equal("", response.SettingsError);
            Assert.Equal("", response.ConnectionError);
            Assert.Equal("", response.OauthError);

            // cleanup
            await channel.ShutdownAsync();
            await server.ShutdownAsync();
        }
        
        [Fact]
        public async Task ConnectFailedTest()
        {
            // setup
            Server server = new Server
            {
                Services = {Publisher.BindService(new PluginOracleNetConfig.Plugin.Plugin())},
                Ports = {new ServerPort("localhost", 0, ServerCredentials.Insecure)}
            };
            server.Start();

            var port = server.Ports.First().BoundPort;

            var channel = new Channel($"localhost:{port}", ChannelCredentials.Insecure);
            var client = new Publisher.PublisherClient(channel);

            const string wrongUsername = "ITSALLWRONG";

            var request = new ConnectRequest
            {
                SettingsJson = JsonConvert.SerializeObject(new Settings
                {
                    Hostname = SettingsHostname,
                    Port = SettingsPort,
                    Password = SettingsPassword,
                    Username = wrongUsername,
                    ServiceName = SettingsServiceName,
                    ConfigSchemaFilePath = TestConfigSchemaFilePath
                }),
                OauthConfiguration = new OAuthConfiguration(),
                OauthStateJson = ""
            };

            // act
            var response = client.Connect(request);

            // assert
            Assert.IsType<ConnectResponse>(response);
            Assert.Equal("", response.SettingsError);
            Assert.Equal($"ORA-01017: invalid username/password; logon denied", response.ConnectionError);
            Assert.Equal("", response.OauthError);

            // cleanup
            await channel.ShutdownAsync();
            await server.ShutdownAsync();
        }
        
        [Fact]
        public async Task ConnectPropsMissingTest()
        {
            // setup
            Server server = new Server
            {
                Services = {Publisher.BindService(new PluginOracleNetConfig.Plugin.Plugin())},
                Ports = {new ServerPort("localhost", 0, ServerCredentials.Insecure)}
            };
            server.Start();

            var port = server.Ports.First().BoundPort;

            var channel = new Channel($"localhost:{port}", ChannelCredentials.Insecure);
            var client = new Publisher.PublisherClient(channel);

            var propsMissingSettings = GetSettings();
            propsMissingSettings.ConfigSchemaFilePath = PropsMissingConfigSchemaFilePath;

            var request = new ConnectRequest
            {
                SettingsJson = JsonConvert.SerializeObject(propsMissingSettings),
                OauthConfiguration = new OAuthConfiguration(),
                OauthStateJson = ""
            };

            // act
            var response = client.Connect(request);

            // assert
            Assert.IsType<ConnectResponse>(response);
            Assert.Equal("Query #3 in the configuration file is missing the property 'query'.", response.SettingsError);
            Assert.Equal("", response.ConnectionError);
            Assert.Equal("", response.OauthError);

            // cleanup
            await channel.ShutdownAsync();
            await server.ShutdownAsync();
        }
        
        [Fact]
        public async Task ConnectDuplicateIdsTest()
        {
            // setup
            Server server = new Server
            {
                Services = {Publisher.BindService(new PluginOracleNetConfig.Plugin.Plugin())},
                Ports = {new ServerPort("localhost", 0, ServerCredentials.Insecure)}
            };
            server.Start();

            var port = server.Ports.First().BoundPort;

            var channel = new Channel($"localhost:{port}", ChannelCredentials.Insecure);
            var client = new Publisher.PublisherClient(channel);

            var propsMissingSettings = GetSettings();
            propsMissingSettings.ConfigSchemaFilePath = DuplicateIdsConfigSchemaFilePath;

            var request = new ConnectRequest
            {
                SettingsJson = JsonConvert.SerializeObject(propsMissingSettings),
                OauthConfiguration = new OAuthConfiguration(),
                OauthStateJson = ""
            };

            // act
            var response = client.Connect(request);

            // assert
            Assert.IsType<ConnectResponse>(response);
            Assert.Equal("2 queries in the configuration file contain duplicate id 'Query1'.", response.SettingsError);
            Assert.Equal("", response.ConnectionError);
            Assert.Equal("", response.OauthError);

            // cleanup
            await channel.ShutdownAsync();
            await server.ShutdownAsync();
        }

        [Fact]
        public async Task DiscoverSchemasAllTest()
        {
            // setup
            Server server = new Server
            {
                Services = { Publisher.BindService(new PluginOracleNetConfig.Plugin.Plugin()) },
                Ports = { new ServerPort("localhost", 0, ServerCredentials.Insecure) }
            };
            server.Start();

            var port = server.Ports.First().BoundPort;

            var channel = new Channel($"localhost:{port}", ChannelCredentials.Insecure);
            var client = new Publisher.PublisherClient(channel);

            var connectRequest = GetConnectSettings();

            var request = new DiscoverSchemasRequest
            {
                Mode = DiscoverSchemasRequest.Types.Mode.All,
                SampleSize = 10
            };

            // act
            UpdateConfigFile();
            client.Connect(connectRequest);
            var response = client.DiscoverSchemas(request);

            // assert
            Assert.IsType<DiscoverSchemasResponse>(response);
            Assert.Equal(2, response.Schemas.Count);

            // --- Detect First Column in testing table ---
            //var schema = response.Schemas[0];
            var schema = response.Schemas[0]; // Use testing table

            Assert.Equal(TestSchemaID, schema.Id);
            Assert.Equal(TestSchemaName, schema.Name);
            Assert.Equal(TestSchemaQuery, schema.Query);
            Assert.Equal("", schema.Description);
            Assert.Equal(TestSampleCount, schema.Sample.Count);
            Assert.Equal(TestPropertyCount, schema.Properties.Count);

            var property = schema.Properties[0];
            Assert.Equal(TestPropertyID, property.Id);
            Assert.Equal(TestPropertyName, property.Name);
            Assert.Equal("", property.Description);
            Assert.Equal(PropertyType.String, property.Type);
            Assert.False(property.IsKey);
            Assert.True(property.IsNullable);
            
            // // --- Detect Primary Key in last table ---
            // // Use the test schema
            // var schema2 = response.Schemas.Single(s => s.Id == TestSchemaID_2);
            //
            // Assert.Equal(TestSchemaID_2, schema2.Id);
            // Assert.Equal(TestSchemaName_2, schema2.Name);
            // Assert.Equal($"", schema2.Query);
            // Assert.Equal(TestSampleCount_2, schema2.Sample.Count);
            // Assert.Equal(TestPropertyCount_2, schema2.Properties.Count);
            //
            // var property2 = schema2.Properties[0];
            // Assert.Equal(TestPropertyID, property2.Id);
            // Assert.Equal(TestPropertyName, property2.Name);
            // Assert.Equal("", property2.Description);
            // Assert.Equal(PropertyType.String, property2.Type);
            // Assert.True(property2.IsKey);
            // Assert.False(property2.IsNullable);

            // cleanup
            await channel.ShutdownAsync();
            await server.ShutdownAsync();
        }

        [Fact]
        public async Task DiscoverSchemasRefreshQueryTest()
        {
            // setup
            Server server = new Server
            {
                Services = { Publisher.BindService(new PluginOracleNetConfig.Plugin.Plugin()) },
                Ports = { new ServerPort("localhost", 0, ServerCredentials.Insecure) }
            };
            server.Start();

            int port = server.Ports.First().BoundPort;

            Channel channel = new Channel($"localhost:{port}", ChannelCredentials.Insecure);
            Publisher.PublisherClient client = new Publisher.PublisherClient(channel);

            var connectRequest = GetConnectSettings();

            var request = new DiscoverSchemasRequest
            {
                Mode = DiscoverSchemasRequest.Types.Mode.Refresh,
                SampleSize = 10,
                ToRefresh = { GetTestSchema(TestSchemaID, TestSchemaName) }
            };

            // act
            UpdateConfigFile();
            client.Connect(connectRequest);
            var response = client.DiscoverSchemas(request);

            // assert
            Assert.IsType<DiscoverSchemasResponse>(response);
            Assert.Single(response.Schemas);

            var schema = response.Schemas[0];
            Assert.Equal(TestSchemaID, schema.Id);
            Assert.Equal(TestSchemaName, schema.Name);
            Assert.Equal(TestSchemaQuery, schema.Query);
            Assert.Equal("", schema.Description);
            Assert.Equal(TestSampleCount, schema.Sample.Count);
            Assert.Equal(TestPropertyCount, schema.Properties.Count);

            var property = schema.Properties[0];
            Assert.Equal(TestPropertyID, property.Id);
            Assert.Equal(TestPropertyName, property.Name);
            Assert.Equal("", property.Description);
            Assert.Equal(PropertyType.String, property.Type);
            Assert.False(property.IsKey);
            Assert.True(property.IsNullable);

            // cleanup
            await channel.ShutdownAsync();
            await server.ShutdownAsync();
        }

        [Fact]
        public async Task DiscoverSchemasRefreshConfigChangeTest()
        {
            // setup
            Server server = new Server
            {
                Services = { Publisher.BindService(new PluginOracleNetConfig.Plugin.Plugin()) },
                Ports = { new ServerPort("localhost", 0, ServerCredentials.Insecure) }
            };
            server.Start();

            int port = server.Ports.First().BoundPort;

            Channel channel = new Channel($"localhost:{port}", ChannelCredentials.Insecure);
            Publisher.PublisherClient client = new Publisher.PublisherClient(channel);

            var connectRequest = GetConnectSettings();

            var request = new DiscoverSchemasRequest
            {
                Mode = DiscoverSchemasRequest.Types.Mode.Refresh,
                SampleSize = 10,
                ToRefresh = { GetTestSchema(TestSchemaID, TestSchemaName) }
            };

            // act
            UpdateConfigFile();
            client.Connect(connectRequest);
            client.DiscoverSchemas(request);
            
            // ---- change the file ----
            UpdateConfigFile(true);
            var response = client.DiscoverSchemas(request);

            // assert
            Assert.IsType<DiscoverSchemasResponse>(response);
            Assert.Single(response.Schemas);

            var schema = response.Schemas[0];
            Assert.Equal(TestSchemaID, schema.Id);
            Assert.Equal(TestSchemaName, schema.Name);
            Assert.Equal(TestSchemaQuery2, schema.Query);
            Assert.Equal("", schema.Description);
            Assert.Equal(TestSampleCount, schema.Sample.Count);
            Assert.Equal(TestPropertyCount2, schema.Properties.Count);

            // load third property, expect it to be email
            var property = schema.Properties[3];
            Assert.Equal(TestPropertyID2, property.Id);
            Assert.Equal(TestPropertyName2, property.Name);
            Assert.Equal("", property.Description);
            Assert.Equal(PropertyType.String, property.Type);
            Assert.False(property.IsKey);
            Assert.True(property.IsNullable);

            // cleanup
            UpdateConfigFile();
            await channel.ShutdownAsync();
            await server.ShutdownAsync();
        }

        [Fact]
        public async Task ReadStreamQuerySchemaTest()
        {
            // setup
            Server server = new Server
            {
                Services = { Publisher.BindService(new PluginOracleNetConfig.Plugin.Plugin()) },
                Ports = { new ServerPort("localhost", 0, ServerCredentials.Insecure) }
            };
            server.Start();

            var port = server.Ports.First().BoundPort;

            var channel = new Channel($"localhost:{port}", ChannelCredentials.Insecure);
            var client = new Publisher.PublisherClient(channel);

            var schema = GetTestSchema(TestSchemaID, TestSchemaName);

            var connectRequest = GetConnectSettings();

            var schemaRequest = new DiscoverSchemasRequest
            {
                Mode = DiscoverSchemasRequest.Types.Mode.Refresh,
                ToRefresh = { schema }
            };

            var request = new ReadRequest()
            {
                DataVersions = new DataVersions
                {
                    JobId = "test"
                },
                JobId = "test",
            };

            // act
            client.Connect(connectRequest);
            var schemasResponse = client.DiscoverSchemas(schemaRequest);
            request.Schema = schemasResponse.Schemas[0];

            var response = client.ReadStream(request);
            var responseStream = response.ResponseStream;
            var records = new List<Naveego.Sdk.Plugins.Record>();

            while (await responseStream.MoveNext())
            {
                records.Add(responseStream.Current);
            }

            // assert
            Assert.Equal(424, records.Count);

            var record = JsonConvert.DeserializeObject<Dictionary<string, object>>(records[0].DataJson);
            Assert.Equal("dc6fdfef-812c-4c98-93cd-a4f839416c99", record["\"ID\""]);
            Assert.Equal("Arlette", record["\"FIRST_NAME\""]);
            Assert.Equal("Stopher", record["\"LAST_NAME\""]);

            // cleanup
            await channel.ShutdownAsync();
            await server.ShutdownAsync();
        }

        [Fact]
        public async Task ReadStreamLimitTest()
        {
            // setup
            Server server = new Server
            {
                Services = { Publisher.BindService(new PluginOracleNetConfig.Plugin.Plugin()) },
                Ports = { new ServerPort("localhost", 0, ServerCredentials.Insecure) }
            };
            server.Start();

            var port = server.Ports.First().BoundPort;

            var channel = new Channel($"localhost:{port}", ChannelCredentials.Insecure);
            var client = new Publisher.PublisherClient(channel);

            var schema = GetTestSchema(TestSchemaID, TestSchemaName, "");

            var connectRequest = GetConnectSettings();

            var schemaRequest = new DiscoverSchemasRequest
            {
                Mode = DiscoverSchemasRequest.Types.Mode.Refresh,
                ToRefresh = { schema }
            };

            var request = new ReadRequest()
            {
                DataVersions = new DataVersions
                {
                    JobId = "test"
                },
                JobId = "test",
                Limit = 10
            };

            // act
            client.Connect(connectRequest);
            var schemasResponse = client.DiscoverSchemas(schemaRequest);
            request.Schema = schemasResponse.Schemas[0];

            var response = client.ReadStream(request);
            var responseStream = response.ResponseStream;
            var records = new List<Naveego.Sdk.Plugins.Record>();

            while (await responseStream.MoveNext())
            {
                records.Add(responseStream.Current);
            }

            // assert
            Assert.Equal(10, records.Count);

            // cleanup
            await channel.ShutdownAsync();
            await server.ShutdownAsync();
        }
    }
}
