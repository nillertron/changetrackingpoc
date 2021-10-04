using System.Data.SqlClient;

namespace ChangeTrackingPoc
{
    public class Context
    {
        private readonly string ConnectionString;

        public SqlConnection Connection => new(ConnectionString);

        public int TenantId { get; }

        public Context(string connectionString, int tenantId)
        {
            ConnectionString = connectionString;
            TenantId = tenantId;
        }
    }
}
