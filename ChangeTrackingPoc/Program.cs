using System;
using System.Threading.Tasks;

namespace ChangeTrackingPoc
{
    class Program
    {
        private static async Task Main(string[] args)
        {
            var target = new Context("Data Source=USD1771;Initial Catalog=ÁdventureWorks2014_sync2;Integrated Security=True;Connect Timeout=30;Encrypt=False;TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False", 0);

            var sender1 = new Context("Data Source=USD1771;Initial Catalog=AdventureWorks2014;Integrated Security=True;Connect Timeout=30;Encrypt=False;TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False", 1);
            var sender2 = new Context("Data Source=USD1771;Initial Catalog=AdventureWorks2014_2;Integrated Security=True;Connect Timeout=30;Encrypt=False;TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False", 2);
            var sender3 = new Context("Data Source=USD1771;Initial Catalog=AdventureWorks2014_3;Integrated Security=True;Connect Timeout=30;Encrypt=False;TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False", 3);
            var sender4 = new Context("Data Source=USD1771;Initial Catalog=AdventureWorks2014_4;Integrated Security=True;Connect Timeout=30;Encrypt=False;TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False", 4);
            var sender5 = new Context("Data Source=USD1771;Initial Catalog=AdventureWorks2014_5;Integrated Security=True;Connect Timeout=30;Encrypt=False;TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False", 5);
            var sender6 = new Context("Data Source=USD1771;Initial Catalog=AdventureWorks2014_6;Integrated Security=True;Connect Timeout=30;Encrypt=False;TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False", 6);
            var sender7 = new Context("Data Source=USD1771;Initial Catalog=AdventureWorks2014_7;Integrated Security=True;Connect Timeout=30;Encrypt=False;TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False", 7);
            var sender8 = new Context("Data Source=USD1771;Initial Catalog=AdventureWorks2014_8;Integrated Security=True;Connect Timeout=30;Encrypt=False;TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False", 8);
            var sender9 = new Context("Data Source=USD1771;Initial Catalog=AdventureWorks2014_9;Integrated Security=True;Connect Timeout=30;Encrypt=False;TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False", 9);
            var sender10 = new Context("Data Source=USD1771;Initial Catalog=AdventureWorks2014_10;Integrated Security=True;Connect Timeout=30;Encrypt=False;TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False", 10);

            var collection = new[] { sender1, sender2, sender3, sender4, sender5, sender6, sender7, sender8, sender9, sender10 };

            var table = new Table("Employee", "BusinessEntityID");
            var table2 = new Table("EmployeeDepartmentHistory", "BusinessEntityID");

            var repo = new Repository.Repository();

            //await repo.InitialSync(new[] { sender1, sender2, sender3, sender4, sender5, sender6, sender7, sender8, sender9, sender10 }, target, new[] { table, table2 });

            var counter = new SyncCounter();
            await counter.Counter(collection, target);

            while (true)
            {
                try
                {
                    await Task.Run(async () =>
                    {
                        await repo.Sync(collection, target, new[] { table, table2 });
                        await Task.Delay(TimeSpan.FromSeconds(60));
                    });
                    Console.WriteLine("Sync cycle done");
                }
                catch (Exception e)
                {
                    await Logger.LogAsync(e, "Main", string.Empty);
                    Console.WriteLine(e);
                }
            }
        }
    }
}
