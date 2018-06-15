﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WebsocketPipe;

namespace CSCom
{
    /// <summary>
    /// Imeplements a CS communication that allows for namepath data sending.
    /// </summary>
    public class CSCom : IDisposable
    {
        /// <summary>
        /// Create a new com service that can connect or listen at the comServiceAddress
        /// </summary>
        /// <param name="comServiceAddress">The addres of the service, the schema must be ws://. i.e. "ws://localhost:50000/CScom"</param>
        public CSCom(string comServiceAddress = "ws://localhost:50000/CSCom")
        {
            Pipe = new WebsocketPipe<NPMessage>(new Uri(comServiceAddress));
            Pipe.Timeout = 30000;

            BindMessageHandling();
        }


        private void CallLogEvent(string websocketID, string s)
        {
            if (DoLogging && Log != null)
                Log(this, new LogEventArgs(websocketID, s));
        }

        ~CSCom()
        {
            try
            {
                Pipe.Stop();
                SendCloseMessage();
            }
            catch { }
        }


        #region Properties

        /// <summary>
        /// The websocket pipe to use.
        /// </summary>
        public WebsocketPipe.WebsocketPipe<NPMessage> Pipe { get; private set; }

        /// <summary>
        /// If true then call log events.
        /// </summary>
        public bool DoLogging { get; set; } = false;

        /// <summary>
        /// If true then logs websocket messages.
        /// </summary>
        public bool DoWebsocketLogging
        {
            get
            {
                return Pipe.LogWebsocketMessages;
            }
            set
            {
                Pipe.LogWebsocketMessages = value;
            }
        }

        /// <summary>
        /// If true then connected or listening.
        /// </summary>
        public bool IsAlive { get { return (IsConnected || IsListening); } }

        /// <summary>
        /// True of the current is a server and is listening.
        /// </summary>
        public bool IsListening { get { return Pipe != null && Pipe.IsListening; } }

        /// <summary>
        /// True of a client and is connected.
        /// </summary>
        public bool IsConnected { get { return Pipe != null && Pipe.IsConnected; } }

        /// <summary>
        /// True if a client
        /// </summary>
        public bool IsClient { get { return Pipe != null && Pipe.IsListening; } }

        /// <summary>
        /// True if a server
        /// </summary>
        public bool IsServer { get { return Pipe.IsListening; } }

        /// <summary>
        /// True if the current executing framework executes event asynchronically.
        /// This will affect the wait when requiring a response.
        /// </summary>
        public bool RequiresAsyncEventLock { get; set; } = false;

        /// <summary>
        /// All events that require a response are executed by the order they arrive. In a single threaded api, this might cause a
        /// callback lock! A->B->A.
        /// </summary>
        public bool SingleThreadedResponseExecution { get; set; } = true;

        /// <summary>
        /// The max response stack size.
        /// </summary>
        public int SingleThreadedResponseExecutionMaxStackSize { get; set; } = 100;

        int? m_ASynchroniusEventExecutionTimeout = null;
        /// <summary>
        /// The timeout to wait when doing async event execution.
        /// </summary>
        public int ASynchroniusEventExecutionTimeout
        {
            get
            {
                if (m_ASynchroniusEventExecutionTimeout == null)
                    return Pipe.Timeout;
                return m_ASynchroniusEventExecutionTimeout.Value;
            }
            set { m_ASynchroniusEventExecutionTimeout = value; }
        }

        /// <summary>
        /// The time to wait before timeout, in ms.
        /// </summary>
        public int Timeout
        {
            get
            {
                return Pipe.Timeout;
            }
            set
            {
                Pipe.Timeout = value;
            }
        }

        /// <summary>
        /// The id of the pipe.
        /// </summary>
        public string ID
        {
            get
            {
                return Pipe.PipeID;
            }
        }

        #endregion

        #region Messages and message handling

        void BindMessageHandling()
        {
            Pipe.LogMethod = (id, s) =>
            {
                CallLogEvent(id, s);
            };

            Pipe.MessageRecived += (s, e) =>
            {
                ProcessMessage(e);
            };

            Pipe.Close += (s, e) =>
            {
                SendCloseMessage(e.WebsocketID);
            };

            Pipe.Error += (s, e) =>
            {
                SendErrorMessage(e.WebsocketID, e.Error);
            };

            Pipe.Ping += (s, e) =>
            {
                if (Ping != null)
                    Ping(this, new PingEventArgs(e.WebsocketID));
            };
        }

        /// <summary>
        /// Sends a close message to the handler.
        /// </summary>
        /// <param name="hndlId">If empty close all handlers</param>
        private void SendCloseMessage(string hndlId = "")
        {
            if (this.MessageRecived == null)
                return;

            // Call the close message.
            this.MessageRecived(this, new WebsocketPipe<NPMessage>.MessageEventArgs(new NPMessage(NPMessageType.Destroy, null, null)
                , false, hndlId));
        }

        private void SendErrorMessage(string id, Exception ex)
        {
            MessageRecived(this,
                new WebsocketPipe<NPMessage>.MessageEventArgs(NPMessage.FromValue(ex.ToString(), NPMessageType.Error, ex.ToString()), false, id));
        }

        Dictionary<string, Queue<WebsocketPipe<NPMessage>.MessageEventArgs>> m_pendingResponseMessagesByID =
            new Dictionary<string, Queue<WebsocketPipe<NPMessage>.MessageEventArgs>>();

        /// <summary>
        /// Called to process a message
        /// </summary>
        /// <param name="e"></param>
        /// <param name="waitingForAsyncEvents"></param>
        private void ProcessMessage(WebsocketPipe<NPMessage>.MessageEventArgs e)
        {
            if (this.MessageRecived == null)
                return;

            if (!e.RequiresResponse)
            {
                this.MessageRecived(this, e);
                return;
            }

            string wsid = e.WebsocketID;

            if (!SingleThreadedResponseExecution)
            {
                try
                {
                    this.MessageRecived(this, e);
                }
                catch (Exception ex)
                {
                    SendErrorMessage(wsid, ex);
                }
                finally
                {
                    if (RequiresAsyncEventLock)
                        e.WaitForAsynchroniusEvent(true, ASynchroniusEventExecutionTimeout);
                }
                return;
            }

            if (m_pendingResponseMessagesByID.ContainsKey(wsid))
            {
                // push to end and return.
                if (m_pendingResponseMessagesByID[wsid].Count > SingleThreadedResponseExecutionMaxStackSize)
                    throw new StackOverflowException("Reached the maximal number of pending requests that require a response.");

                m_pendingResponseMessagesByID[wsid].Enqueue(e);
                return;
            }
            
            m_pendingResponseMessagesByID[wsid] = new Queue<WebsocketPipe<NPMessage>.MessageEventArgs>();
            m_pendingResponseMessagesByID[wsid].Enqueue(e);

            while(m_pendingResponseMessagesByID[wsid].Count>0)
            {
                e = m_pendingResponseMessagesByID[wsid].Dequeue();
                try
                {
                    this.MessageRecived(this, e);
                }
                catch (Exception ex)
                {
                    SendErrorMessage(wsid, ex);
                }
                finally
                {
                    if (RequiresAsyncEventLock)
                    {
                        e.WaitForAsynchroniusEvent(true, ASynchroniusEventExecutionTimeout);
                    }
                }
            }

            lock (m_pendingResponseMessagesByID)
            {
                m_pendingResponseMessagesByID.Remove(wsid);
            }

            //if (this.RequiresAsyncEventLock)
            //{
            //    if (waitingForAsyncEvents.ContainsKey(e.WebsocketID))
            //    {
            //        var lastEv = waitingForAsyncEvents[e.WebsocketID];
            //        throw new Exception("Called handler " + e.WebsocketID + " to wait for async events though already waiting for async event." +
            //            " \nPending message: " + (lastEv.Message != null ? lastEv.Message.ToString() : "[none]") +
            //            " \nCurrent message: " + (e.Message != null ? e.Message.ToString() : "[none]"));
            //    }
            //    waitingForAsyncEvents[e.WebsocketID] = e;
            //}
            //if (this.MessageRecived != null)
            //    this.MessageRecived(this, e);

            //if (this.RequiresAsyncEventLock)
            //{
            //    e.WaitForAsynchroniusEvent(true, ASynchroniusEventExecutionTimeout);
            //    waitingForAsyncEvents.Remove(e.WebsocketID);
            //}
        }

        #endregion

        #region Events

        /// <summary>
        /// The log event args.
        /// </summary>
        public class LogEventArgs : EventArgs
        {
            public LogEventArgs(string websocketID, string msg="")
            {
                Message = msg;
                WebsocketID = websocketID;
            }

            public string Message { get; private set; } = "";
            public string WebsocketID { get; private set; }
        }

        /// <summary>
        /// The log event args.
        /// </summary>
        public class PingEventArgs : EventArgs
        {
            public PingEventArgs(string websocketID)
            {
                WebsocketID = websocketID;
            }

            public string WebsocketID { get; private set; }
        }

        /// <summary>
        /// Called on a log event.
        /// </summary>
        public event EventHandler<LogEventArgs> Log;

        /// <summary>
        /// Called on a log event.
        /// </summary>
        public event EventHandler<PingEventArgs> Ping;

        /// <summary>
        /// Called when a message is recived.
        /// </summary>
        public event EventHandler<WebsocketPipe<NPMessage>.MessageEventArgs> MessageRecived;

        #endregion

        #region Com

        /// <summary>
        /// Connect as a client.
        /// </summary>
        public void Connect(bool inANewThread = false)
        {
            if (IsAlive)
            {
                if (!IsConnected)
                    Stop();
                else return;
            }
            if (inANewThread)
            {
                new Task((cscom) =>
                {
                    Pipe.Connect();
                    while (!IsConnected)
                        System.Threading.Thread.Sleep(10);
                    do
                    {
                        System.Threading.Thread.Sleep(10);
                    }
                    while (IsConnected);
                }, this).Start();
                while (!IsConnected)
                    System.Threading.Thread.Sleep(10);
            }
            else Pipe.Connect();
        }

        /// <summary>
        /// Listen as a server.
        /// </summary>
        public void Listen(bool inANewThread = false)
        {
            if (IsAlive)
            {
                if (!IsListening)
                    Stop();
                else return;
            }

            if (inANewThread)
            {
                new Task((cscom) =>
                {
                    Pipe.Listen();
                    while (!IsListening)
                        System.Threading.Thread.Sleep(10);
                    do
                    {
                        System.Threading.Thread.Sleep(10);
                    }
                    while (IsListening);
                }, this).Start();
                while (!IsListening)
                    System.Threading.Thread.Sleep(10);
            }
            else Pipe.Listen();
        }

        /// <summary>
        /// Stop either listening or disconnect from server.
        /// </summary>
        public void Stop()
        {
            Pipe.Stop();
        }

        /// <summary>
        /// Stop and then release resources, this action is unreversable. Use stop if you want to stop the client/server.
        /// </summary>
        public void Dispose()
        {
            Stop();
            Pipe.Dispose();
        }

        #endregion

        #region Specialized operations

        public NPMessage Get(string namepath)
        {
            return Get(new string[] { namepath });
        }

        public NPMessage Get(string[] namepaths)
        {
            NPMessageNamepathData[] data = namepaths.Select(np => new NPMessageNamepathData(np)).ToArray();
            return Send(NPMessageType.Get, "", data, true);
        }

        #endregion

        #region Core send commands

        /// <summary>
        /// Sends a message and waits for response if needed.
        /// </summary>
        /// <param name="msg">The message to send</param>
        /// <param name="requireResponse">If true a response will be required from the other party. Synchronius action.</param>
        /// <param name="toWebsocket">If null then broadcase (as a server, this would mean sending to all clients)</param>
        /// <returns>The response if required, otherwise null.</returns>
        public NPMessage Send(NPMessageType type, string msg, NPMessageNamepathData data, bool requireResponse = false, string toWebsocket = null)
        {
            return Send(type, msg, data == null ? null : new NPMessageNamepathData[1] { data }, requireResponse, toWebsocket);
        }

        /// <summary>
        /// Sends a message and waits for response if needed.
        /// </summary>
        /// <param name="msg">The message to send</param>
        /// <param name="requireResponse">If true a response will be required from the other party. Synchronius action.</param>
        /// <param name="toWebsocket">If null then broadcase (as a server, this would mean sending to all clients)</param>
        /// <returns>The response if required, otherwise null.</returns>
        public NPMessage Send(NPMessageType type, string msg, NPMessageNamepathData[] data, bool requireResponse = false, string toWebsocket = null)
        {
            if(toWebsocket!=null)
            {
                toWebsocket = toWebsocket.Trim();
                toWebsocket = toWebsocket.Length == 0 ? null : toWebsocket;
            }

            return Send(new NPMessage(type, data, msg), requireResponse, toWebsocket);
        }

        /// <summary>
        /// Sends a message and waits for response if needed.
        /// </summary>
        /// <param name="msg">The message to send</param>
        /// <param name="requireResponse">If true a response will be required from the other party. Synchronius action.</param>
        /// <param name="toWebsocket">If null then broadcase (as a server, this would mean sending to all clients)</param>
        /// <returns>The response if required, otherwise null.</returns>
        public NPMessage Send(NPMessage msg, bool requireResponse = false, string toWebsocket = null)
        {
            // empty string is null in this case.
            if (toWebsocket!=null && toWebsocket.Length == 0)
                toWebsocket = null;

            NPMessage response = null;
            if (requireResponse)
                Pipe.Send(msg, toWebsocket, (rsp) =>
                 {
                     response = rsp;
                 });
            else Pipe.Send(msg, toWebsocket);

            return response;
        }

        #endregion

        #region Static Methods

        /// <summary>
        /// Collect all garbage helper.
        /// </summary>
        public static void CollectGarbage()
        {
            // Force garbage collection.
            GC.Collect();
        }

        public static void OpenMatlabFile(string fname)
        {
            if (!System.IO.File.Exists(fname))
                throw new Exception("File dose not exist");
            Task t = new Task(() =>
              {
                  System.Diagnostics.Process.Start(fname);
              });
            
            try
            {
                t.Start();
                t.Wait(10000);
            }
            catch(Exception ex)
            {
                throw ex;
            }
        }

        public void LogNetInfo()
        {
            StringBuilder str = new StringBuilder();
            str.AppendLine("Net version: " + Environment.Version.ToString());
            str.AppendLine("Thread priority: " + System.Threading.Thread.CurrentThread.Priority);
            CallLogEvent(null, str.ToString());
        }

        public static string PrintValueType(object o)
        {
            return o.GetType().ToString();
        }

        /// <summary>
        /// Make a cscom object inside the C# env. (Labview?)
        /// </summary>
        /// <param name="comServiceAddress"></param>
        /// <returns></returns>
        public static CSCom Make(string comServiceAddress = "ws://localhost:50000/CSCom")
        {
            CSCom co = new CSCom(comServiceAddress);
            return co;
        }

        /// <summary>
        /// 
        /// </summary>
        static Dictionary<string, CSCom> sm_StaticRefrences = new Dictionary<string, CSCom>();

        /// <summary>
        /// Registeres a static refrence to id, to allow for the static refrences to be created.
        /// </summary>
        /// <param name="refrenceID"></param>
        /// <param name="com"></param>
        public static void RetisterStaticRefrenceToID(string refrenceID, CSCom com)
        {
            DestroyStaticRefrenceById(refrenceID);
            sm_StaticRefrences[refrenceID] = com;
        }

        /// <summary>
        /// Destroy the static refrence and close all connections.
        /// </summary>
        /// <param name="refrenceID"></param>
        public static void DestroyStaticRefrenceById(string refrenceID)
        {
            if (!sm_StaticRefrences.ContainsKey(refrenceID))
                return;

            CSCom com = sm_StaticRefrences[refrenceID];
            sm_StaticRefrences.Remove(refrenceID);

            // send the destroy command.
            if (com.IsAlive)
            {
                com.Send(NPMessage.FromValue(null, NPMessageType.Destroy));
                com.Stop();
            }
        }

        #endregion
    }
}
