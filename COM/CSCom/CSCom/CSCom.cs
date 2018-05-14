using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

            Pipe.LogMethod = (d, s) =>
            {
                if (DoLogging && Log != null)
                    Log(this, new LogEventArgs(d.ToString()));
            };

            Pipe.MessageRecived += (s, e) =>
            {
                if (this.MessageRecived != null)
                    this.MessageRecived(this, e);

                if(this.ASynchroniusEventExecution && e.RequiresResponse)
                {
                    e.WaitForAsynchroniusEvent(true, ASynchroniusEventExecutionTimeout);
                }
            };

            Pipe.Close += (s, e) =>
            {
                if (this.MessageRecived == null)
                    return;

                NPMessage closeMsg = new NPMessage(NPMessageType.Destroy, null, e.WebsocketID);

                // replace me message object and send on.
                e.Message = closeMsg;

                // Call the close message.
                this.MessageRecived(this, e);
            };
        }

        ~CSCom()
        {
            try
            {
                Pipe.Stop();
            }
            catch
            {
            }
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
        /// If true then connected or listening.
        /// </summary>
        public bool IsAlive { get { return Pipe != null && (IsConnected || IsListening); } }

        /// <summary>
        /// True of the current is a server and is listening.
        /// </summary>
        public bool IsListening { get { return Pipe != null && IsServer && Pipe.WSServer.IsListening; } }

        /// <summary>
        /// True of a client and is connected.
        /// </summary>
        public bool IsConnected { get { return Pipe != null && IsClient && Pipe.WS.ReadyState == WebSocketSharp.WebSocketState.Open; } }

        /// <summary>
        /// True if a client
        /// </summary>
        public bool IsClient { get { return Pipe != null && Pipe.WS != null; } }

        /// <summary>
        /// True if a server
        /// </summary>
        public bool IsServer { get { return Pipe != null && Pipe.WSServer != null; } }

        /// <summary>
        /// True if the current executing framework dose asynchronius event execution.
        /// This will affect the wait when requiring a response.
        /// </summary>
        public bool ASynchroniusEventExecution { get; set; } = false;

        /// <summary>
        /// The timeout to wait when doing async event execution.
        /// </summary>
        public int ASynchroniusEventExecutionTimeout { get; set; } = 10000;

        #endregion

        #region Events

        /// <summary>
        /// The log event args.
        /// </summary>
        public class LogEventArgs : EventArgs
        {
            public LogEventArgs(string msg="")
            {
                Message = msg;
            }

            public string Message { get; private set; } = "";
        }

        /// <summary>
        /// Called on a log event.
        /// </summary>
        public event EventHandler<LogEventArgs> Log;

        /// <summary>
        /// Called when a message is recived.
        /// </summary>
        public event EventHandler<WebsocketPipe<NPMessage>.MessageEventArgs> MessageRecived;

        #endregion

        #region Com

        /// <summary>
        /// Connect as a client.
        /// </summary>
        public void Connect()
        {
            Pipe.Connect();
        }

        /// <summary>
        /// Listen as a server.
        /// </summary>
        public void Listen()
        {
            Pipe.Listen();
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
    }
}
