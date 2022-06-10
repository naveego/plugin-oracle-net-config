using PluginOracleNet.API.Utility;

namespace PluginOracleNet.DataContracts
{
    public class WriteStoredProcedure
    {
        public string SchemaName { get; set; }
        public string ProcedureName { get; set; }
        public string ProcedureId { get; set; }

        public string GetId()
        {
            return $"{Utility.GetSafeName(SchemaName.ToAllCaps())}.{Utility.GetSafeName(ProcedureId)}";
        }
        
        public string GetName()
        {
            return $"{Utility.GetSafeName(SchemaName.ToAllCaps())}.{Utility.GetSafeName(ProcedureName)}";
        }
    }
}