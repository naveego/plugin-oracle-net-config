using System.Data;
using PluginOracleNet.Helper;

namespace PluginOracleNet.API.Factory
{
    public class ConnectionFactory : IConnectionFactory
    {
        private Settings _settings;

        public void Initialize(Settings settings)
        {
            _settings = settings;
        }

        public IConnection GetConnection()
        {
            return new Connection(_settings);
        }
        
        public ICommand GetCommand(string commandText, IConnection connection)
        {
            return new Command(commandText, connection);
        }
    }
}