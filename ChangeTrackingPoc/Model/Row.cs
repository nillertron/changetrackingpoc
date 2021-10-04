using System.Collections.Generic;

namespace ChangeTrackingPoc
{
    class Row
    {
        public Row(IEnumerable<object> values)
        {
            Values = values;
        }

        public IEnumerable<object> Values { get; }
    }
}
