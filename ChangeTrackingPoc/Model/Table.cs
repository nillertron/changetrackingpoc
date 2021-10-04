using System.Collections.Generic;

namespace ChangeTrackingPoc
{
    class Table
    {
        public Table(string name, string primaryKeyName, IEnumerable<Column> columns = null)
        {
            Name = name;
            PrimaryKeyName = primaryKeyName;
            Columns = columns;
        }

        public string Name { get; }
        public string PrimaryKeyName { get; }

        // If empty assume all columns needs sync
        public IEnumerable<Column> Columns { get; }
        public IEnumerable<Row> Rows { get; set; }
    }
}
