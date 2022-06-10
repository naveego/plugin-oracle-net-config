using System.Data;
using PluginOracleNet.Helper;

namespace PluginOracleNet.API.Factory
{
    public interface IConnectionFactory
    {
        void Initialize(Settings settings);
        IConnection GetConnection();
        ICommand GetCommand(string commandText, IConnection conn);
    }
}