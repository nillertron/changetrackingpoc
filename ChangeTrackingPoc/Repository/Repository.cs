using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChangeTrackingPoc.Model;
using Dapper;

namespace ChangeTrackingPoc.Repository
{
    internal class Repository
    {
        public async Task InitialSync(IEnumerable<Context> fromContexts, Context targetContext, IEnumerable<Table> tables)
        {
            foreach (var context in fromContexts)
            {
                await EnableChangeTracking(context, tables);
                await InitSyncContextAsync(context, targetContext, tables);
            }
        }

        private async Task InitSyncContextAsync(Context fromContext, Context toContext, IEnumerable<Table> tables)
        {
            foreach (var table in tables)
            {
                var deleteExisitingDataQuery = await GenerateDeleteAllForTenantQueryAsync(fromContext.TenantId, table);
                await ExecuteQueryAsync(toContext, table, deleteExisitingDataQuery);

                //get all data + tilføj tenant id
                var data = await QueryAsync(fromContext, $"select * from {table.Name}");

                //Insert data
                if (data == null)
                    return;

                var query = new StringBuilder();

                foreach (var entry in data)
                {
                    query.Append(await GenerateInsertQuery(MapDbObjectToDictionary(Convert.ToString(entry)),
                        fromContext.TenantId,
                        table));
                }

                await ExecuteQueryAsync(toContext, table, query.ToString());
            }
        }

        public async Task Sync(IEnumerable<Context> fromContexts, Context targetContext, IEnumerable<Table> tables)
        {
            foreach (var context in fromContexts)
                await SyncContext(context, targetContext, tables);
        }


        private async Task EnableChangeTracking(Context context, IEnumerable<Table> tables)
        {
            foreach (var table in tables)
            {
                //var sql = $"ALTER TABLE {table.Name}\r\nENABLE CHANGE_TRACKING\r\nWITH (TRACK_COLUMNS_UPDATED = ON)";
                var sql =
                    $"IF NOT EXISTS (SELECT 1 FROM sys.change_tracking_tables \r\n               WHERE object_id = OBJECT_ID('{table.Name}'))\r\nBEGIN\r\n     ALTER TABLE {table.Name}\r\n     ENABLE CHANGE_TRACKING\r\n     WITH (TRACK_COLUMNS_UPDATED = OFF)\r\nEND";
                using (context.Connection)
                {
                    await context.Connection.ExecuteAsync(sql);
                }
            }
        }

        private async Task SyncContext(Context fromContext, Context toContext, IEnumerable<Table> tables)
        {
            foreach (var table in tables)
            {
                var tableChanges = await GetChangedIdsForTable(fromContext, table);

                var getCreatedQuery = await BuildQuery(table, tableChanges.Where(x => x.Operation == Cud.Create));
                var getUpdatedQuery = await BuildQuery(table, tableChanges.Where(x => x.Operation == Cud.Update));


                var createdEntities = await QueryAsync(fromContext, getCreatedQuery?.QueryForTable);
                var updatedEntities = await QueryAsync(fromContext, getUpdatedQuery?.QueryForTable);
                var deletedEntities = tableChanges.Where(x => x.Operation == Cud.Delete);


                if (createdEntities != null)
                {
                    var insertQuery = new StringBuilder();
                    foreach (var entity in createdEntities)
                    {
                        insertQuery.Append(await GenerateInsertQuery(MapDbObjectToDictionary(Convert.ToString(entity)),
                            fromContext.TenantId,
                            table));
                    }

                    await ExecuteQueryAsync(toContext, table, insertQuery.ToString());
                    Console.WriteLine($"Inserted {createdEntities.Count()} rows in table {table.Name} for tenant {fromContext.TenantId}");
                }

                if (updatedEntities != null)
                {
                    var updateQuery = new StringBuilder();
                    foreach (var entity in updatedEntities)
                    {
                        updateQuery.Append(await GenerateUpdateQueryAsync(MapDbObjectToDictionary(Convert.ToString(entity)),
                            fromContext.TenantId,
                            table));
                    }

                    await ExecuteQueryAsync(toContext, table, updateQuery.ToString());
                    Console.WriteLine($"Updated {updatedEntities.Count()} rows in table {table.Name} for tenant {fromContext.TenantId}");
                }

                if (deletedEntities != null)
                {
                    var deleteQuery = new StringBuilder();
                    foreach (var entity in deletedEntities)
                    {
                        deleteQuery.Append(await GenerateDeleteQueryAsync(entity.Id, fromContext, table));
                    }

                    if (deletedEntities.Any())
                    {
                        await ExecuteQueryAsync(toContext, table, deleteQuery.ToString());
                        Console.WriteLine($"Deleted {deletedEntities.Count()} rows in table {table.Name} for tenant {fromContext.TenantId}");
                    }
                }
            }
        }

        private async Task ExecuteQueryAsync(Context context, Table table, string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return;
            using (context.Connection)
            {
                try
                {
                    var identityOffQuery = $"SET IDENTITY_INSERT {table.Name} ON; \n" + query + $"SET IDENTITY_INSERT {table.Name} OFF;";
                    await context.Connection.ExecuteAsync(identityOffQuery);
                }
                catch (Exception e)
                {
                    var sqlException = e as SqlException;
                    // Identity is not set to OFF by default, so we execute the query without
                    if (sqlException.Number == 8106)
                        await context.Connection.ExecuteAsync(query);
                    else
                        await Logger.LogAsync(e, "ExecuteQueryAsync", query);
                }
            }
        }

        private Dictionary<string, string> MapDbObjectToDictionary(string entity)
        {
            entity = entity.Replace("{", "");
            entity = entity.Replace("DapperRow,", "");
            entity = entity.Replace("}", "");
            var dict = new Dictionary<string, string>();
            var attributes = entity.Split(',');
            foreach (var attribute in attributes)
            {
                var splitted = attribute.Split('=');

                if (splitted[0].Trim().ToLower() == "RemoveThisId".ToLower())
                    continue;

                dict.Add(splitted[0], splitted[1]);
            }

            return dict;
        }

        private Task<string> GenerateInsertQuery(Dictionary<string, string> keyValues,
            int tenantId,
            Table table)
        {
            var sb = new StringBuilder();
            sb.Append($"insert into {table.Name} ");
            sb.Append("(");
            foreach (var key in keyValues)
            {
                sb.Append(key.Key);
                sb.Append(", ");
            }

            sb.Append("TenantId");

            sb.Append(")");
            sb.Append("values");
            sb.Append("(");
            foreach (var value in keyValues)
            {
                if (DateTime.TryParse(value.Value.Replace("\'", ""), out var dt))
                    sb.Append("'" + dt.ToString("s") + "'");
                else
                    sb.Append(value.Value);


                sb.Append(", ");
            }

            sb.Append(tenantId);

            sb.Append(");\n");
            return Task.FromResult(sb.ToString());
        }

        private Task<string> GenerateUpdateQueryAsync(Dictionary<string, string> keyValues,
            int tenantId,
            Table table)
        {
            var sb = new StringBuilder();

            var primaryKeyValue = keyValues.First(x => x.Key.ToLower().Contains(table.PrimaryKeyName.ToLower()));
            //Ikke muligt at opdatere primary key
            keyValues.Remove(primaryKeyValue.Key);

            sb.Append($"Update {table.Name} ");
            sb.Append("set ");
            var count = 0;
            foreach (var entry in keyValues)
            {
                count++;
                sb.Append(entry.Key);
                sb.Append("=");

                if (DateTime.TryParse(entry.Value.Replace("\'", ""), out var dt))
                    sb.Append("'" + dt.ToString("s") + "'");
                else
                    sb.Append(entry.Value);

                if (count < keyValues.Count)
                    sb.Append(", ");
                else
                    sb.Append(" ");
            }

            sb.Append($"where {table.PrimaryKeyName} = {primaryKeyValue.Value} and TenantId = {tenantId};\n");

            return Task.FromResult(sb.ToString());
        }

        private Task<string> GenerateDeleteQueryAsync(string id, Context fromContext, Table table)
        {
            return Task.FromResult($"Delete from {table.Name} where {table.PrimaryKeyName} = {id} AND TenantId = {fromContext.TenantId}");
        }

        private Task<string> GenerateDeleteAllForTenantQueryAsync(int tenantId, Table table)
        {
            return Task.FromResult($"Delete from {table.Name} where TenantId = {tenantId};\n");
        }

        private async Task<IEnumerable<dynamic>> QueryAsync(Context context, string query)
        {
            if (string.IsNullOrEmpty(query))
                return null;
            using (context.Connection)
            {
                try
                {
                    return await context.Connection.QueryAsync(query);
                }
                catch (Exception e)
                {
                    await Logger.LogAsync(e, "QueryAsync", query);
                    return new dynamic[] { };
                }
            }
        }

        private async Task<Query> BuildQuery(Table table, IEnumerable<ChangeTable> ids)
        {
            if (!ids.Any())
                return null;
            var sb = new StringBuilder();
            sb.Append("CREATE TABLE #ids(RemoveThisId INT NOT NULL PRIMARY KEY);\r\nINSERT INTO #ids(RemoveThisId) VALUES ");
            var insCount = 0;
            var idsCount = ids.Count();
            foreach (var id in ids)
            {
                sb.Append($"({id.Id})");
                insCount++;
                if (insCount % 1000 == 0 && insCount > 0 && insCount < ids.Count())
                {
                    sb.Append(";\n");
                    sb.Append("INSERT INTO #ids(RemoveThisId) VALUES ");
                    continue;
                }

                if (insCount < idsCount)
                    sb.Append(", ");
                else
                    sb.Append(";");
            }

            sb.Append("select");
            if (table.Columns == null || !table.Columns.Any())
                sb.Append(" * ");
            else
            {
                var count = 0;
                foreach (var attribut in table.Columns)
                {
                    count++;
                    sb.Append($"{attribut.Name}");
                    if (count < table.Columns.Count())
                        sb.Append(",");
                    sb.Append(" ");
                }
            }

            sb.Append($"from {table.Name} as T \n");
            sb.Append("INNER JOIN #ids as ids on\n");
            sb.Append($"ids.RemoveThisId=T.{table.PrimaryKeyName};");
            sb.Append("DROP TABLE #ids;");


            return new Query { QueryForTable = sb.ToString(), Table = table };
        }

        private async Task<IEnumerable<ChangeTable>> GetChangedIdsForTable(Context context, Table table)
        {
            var lastVersion = await GetLastVersionAsync(context.TenantId, table.Name);
            var query = $"SELECT * FROM CHANGETABLE \r\n(CHANGES {table.Name},{lastVersion}) as CT ORDER BY SYS_CHANGE_VERSION";
            using (context.Connection)
            {
                var data = await context.Connection.QueryAsync(query);
                var dataOfInterest = data.Select(x =>
                {
                    var changeTable = new ChangeTable();
                    foreach (var element in x)
                    {
                        if (element.Key.ToString().ToLower().Contains(table.PrimaryKeyName.ToLower()))
                            changeTable.Id = element.Value.ToString();
                        if (element.Key.Contains("SYS_CHANGE_OPERATION"))
                            switch (element.Value.ToString())
                            {
                                case "D":
                                    changeTable.Operation = Cud.Delete;
                                    break;
                                case "I":
                                    changeTable.Operation = Cud.Create;
                                    break;
                                case "U":
                                    changeTable.Operation = Cud.Update;
                                    break;
                            }

                        if (element.Key.Contains("SYS_CHANGE_VERSION"))
                            changeTable.Sys_Change_Version = Convert.ToInt32(element.Value);
                    }

                    return changeTable;
                });

                if (dataOfInterest.Any())
                    await PersistLatestVersionAsync(context.TenantId, dataOfInterest.Max(x => x.Sys_Change_Version), table.Name);
                return dataOfInterest;
            }
        }

        private async Task<int> GetLastVersionAsync(int tenantId, string table)
        {
            // If file doesn't exists, go in catch and return 0.. File will be created after first successfull run
            var max = 0;
            try
            {
                var data = await File.ReadAllLinesAsync("dbversion.txt");
                foreach (var line in data)
                {
                    var split = line.Split(",");
                    if (split[0].Trim() == tenantId.ToString() && split[2].Trim() == table)
                    {
                        var asInteger = Convert.ToInt32(split[1]);
                        if (asInteger > max)
                            max = asInteger;
                    }
                }
            }
            catch (Exception)
            {
            }

            return max;
        }

        private async Task PersistLatestVersionAsync(int tenantId, int version, string tableName)
        {
            await File.AppendAllLinesAsync("dbversion.txt", new[] { $"{tenantId}, {version}, {tableName}" });
        }
    }
}