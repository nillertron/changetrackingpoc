using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;

namespace ChangeTrackingPoc
{
    class SyncCounter
    {
        public async Task Counter(IEnumerable<Context> contexts, Context tarcontext)
        {
            var sum1 = 0;
            var sum2 = 0;
            foreach (var context in contexts)
            {
                using (context.Connection)
                {
                    var dbsum1 = await context.Connection.QueryAsync<int>("select count(*) from Employee");
                    var dbsum2 = await context.Connection.QueryAsync<int>("select count(*) from EmployeeDepartmentHistory");
                    sum1 += dbsum1.First();
                    sum2 += dbsum2.First();
                }
            }

            using (tarcontext.Connection)
            {
                var targetSum1 = (await tarcontext.Connection.QueryAsync<int>("select count(*) from Employee")).First();
                var targetSum2 = (await tarcontext.Connection.QueryAsync<int>("select count(*) from EmployeeDepartmentHistory")).First();
            }
        }
    }
}
