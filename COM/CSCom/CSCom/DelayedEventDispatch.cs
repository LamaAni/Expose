using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSCom
{
    /// <summary>
    /// Helps other non async lagnuage (like matlab) to allow for a delayed event execution.
    /// </summary>
    public class DelayedEventDispatch
    {
        public DelayedEventDispatch()
        {
        }

        public class DEDEventArgs : EventArgs
        {
            public DEDEventArgs(object val=null)
            {
                Value = val;
            }

            public object Value { get; private set; }
        }

        public event EventHandler<DEDEventArgs> Ready;
        public bool BlockMultiCalls { get; private set; } = true;

        protected Queue<DEDEventArgs> eventQueue = new Queue<DEDEventArgs>();
        Task m_eventInvokeTask = null;

        public void Trigger(int delay = 0, object val = null)
        {
            if (Ready == null)
                return;

            if (BlockMultiCalls)
                while (eventQueue.Count > 0)
                    eventQueue.Dequeue();

            eventQueue.Enqueue(new DEDEventArgs(val));

            if (m_eventInvokeTask == null || m_eventInvokeTask.Status != TaskStatus.Running)
            {
                m_eventInvokeTask = new Task(() =>
                  {
                      if (delay > 0)
                          System.Threading.Thread.Sleep(delay);
                      InvokeEvents();
                  });
                m_eventInvokeTask.Start();
            }
        }

        protected void InvokeEvents()
        {
            while (eventQueue.Count > 0)
            {
                var ev = eventQueue.Dequeue();
                if (Ready != null)
                    Ready(this, ev);
            }
            m_eventInvokeTask = null;
        }
    }
}
