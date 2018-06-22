using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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
            LockHanlde = new EventWaitHandle(false, EventResetMode.ManualReset);
        }

        public class DEDEventArgs : EventArgs
        {
            public DEDEventArgs(DateTime insertedAt, int delay, object val = null)
            {
                Value = val;
                InsertedAt = insertedAt;
                Delay = delay;
            }

            public object Value { get; private set; }
            public DateTime InsertedAt { get; private set; }
            public int Delay { get; private set; }
        }

        public event EventHandler<DEDEventArgs> Ready;
        public bool BlockMultiCalls { get; private set; } = true;
        public bool UseThreadLock { get; private set; } = false;
        public int ThreadLockTimeout { get; set; } = 1000;

        protected EventWaitHandle LockHanlde { get; private set; }
        public Exception LastError { get; private set; } = null;
        public int LastErrorIndex { get; private set; } = 0;
        protected Queue<DEDEventArgs> eventQueue = new Queue<DEDEventArgs>();

        Task m_eventInvokeTask = null;
        bool IsDispatchRunning = false;
        object threadCreateLock = new object();
        public void Trigger(int delay = 0, object val = null)
        {
            if (Ready == null)
                return;

            if (BlockMultiCalls && eventQueue.Count > 0)
            {
                return;
            }

            eventQueue.Enqueue(new DEDEventArgs(DateTime.Now, delay, val));

            if (!IsDispatchRunning || m_eventInvokeTask != null && m_eventInvokeTask.Status != TaskStatus.Running)
            {
                lock (threadCreateLock)
                {
                    m_eventInvokeTask = new Task(() =>
                    {
                        InvokeEvents();
                        IsDispatchRunning = false;
                        m_eventInvokeTask = null;
                    });

                    IsDispatchRunning = true;

                    m_eventInvokeTask.Start();
                }
            }
        }

        protected void InvokeEvents()
        {
            while (eventQueue.Count > 0) 
            {
                CallEvent();
            }
        }

        void CallEvent()
        {
            if (Ready == null)
                return;

            DEDEventArgs ev = null;
            lock (eventQueue)
            {
                ev = eventQueue.Peek();
            }

            int totalMsToWait = ev.Delay - (int)Math.Ceiling((DateTime.Now - ev.InsertedAt).TotalMilliseconds);

            if (totalMsToWait > 0)
                System.Threading.Thread.Sleep(totalMsToWait);

            lock (eventQueue)
            {
                // dqueue after the event completed.
                eventQueue.Dequeue();
            }

            if (UseThreadLock)
                LockHanlde = new EventWaitHandle(false, EventResetMode.ManualReset);

            try
            {
                Ready(this, ev);
            }
            catch(Exception ex)
            {
                // nothing here.
                LastError = ex;
                LastErrorIndex += 1;
            }

            if (UseThreadLock)
            {
                try
                {
                    LockHanlde.WaitOne(ThreadLockTimeout);
                }
                catch(Exception ex)
                {
                    LastError = ex;
                    LastErrorIndex += 1;
                }
            }

        }

        public void EventReadyComplete()
        {
            if(UseThreadLock && LockHanlde!=null)
            {
                LockHanlde.Set();
                LockHanlde = null;
            }
        }
    }
}
