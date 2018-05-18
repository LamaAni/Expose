using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CSCom
{
    public class EventBoundReference
    {
        public EventBoundReference(object val)
        {
            val = Value;
        }

        ~EventBoundReference()
        {
            if (RefrenceDestoryed != null)
                RefrenceDestoryed(this, null);
        }

        public event EventHandler RefrenceDestoryed;

        /// <summary>
        /// The value to keep.
        /// </summary>
        public Object Value { get; private set; } = null;

    }
}
