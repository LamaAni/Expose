using CSCom;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tester
{
    class Program
    {
        static int WaitAtServer = 1000;
        static int WaitAtClient = 400;
        static void Main(string[] args)
        {
            bool doSelfServer = true;
            bool waitBeforeStopping = true;
            CSCom.CSCom server = null;
            if (doSelfServer)
            {
                server = new CSCom.CSCom();
                server.DoLogging = true;
                server.DoWebsocketLogging = false;
                server.Log+=(s,e)=>{
                    Console.WriteLine(e.Message);
                };
                server.Listen();
                server.MessageRecived += Server_MessageRecived1; ;
            }

            CSCom.CSCom client = new CSCom.CSCom();
            client.Log += (s, e) => {
                Console.WriteLine(e.Message);
            };

            client.DoLogging = true;
            client.DoWebsocketLogging = false;
            client.Connect(true);
            client.MessageRecived += Clinet_MessageRecived;
            //System.Threading.Thread.Sleep(300);
            
            if(client.IsAlive)
            {
                Console.WriteLine("Connected to server.");
                int imgsize = 3000;
                int n = 10;
                var valToSend = new float[imgsize,imgsize];
                Random r = new Random();
                for (int i = 0; i < imgsize; i++)
                {
                    for (var j = 0; j < imgsize; j++)
                        valToSend[i, j] = (float)r.NextDouble();
                }

                Console.WriteLine("Sending dummy message for first time serialization....");
                Console.WriteLine();
                NPMessage rsp = client.Send(NPMessage.FromValue(new double[10000], NPMessageType.Invoke, "dump"), true);
                Console.WriteLine();

                Stopwatch watch = new Stopwatch();
                Console.WriteLine("Sending " + n + " large messages....");
                Console.WriteLine();
                watch.Start();
                for (int i = 0; i < n; i++)
                    rsp = client.Send(NPMessage.FromValue(valToSend, NPMessageType.Invoke, "lama"), false);
                watch.Stop();
                Console.WriteLine();

                if (waitBeforeStopping || doSelfServer)
                {
                    Console.WriteLine("Waited for send[ms]: " + watch.Elapsed.TotalMilliseconds);
                    if (rsp != null)
                        Console.WriteLine("Recived response text: " + rsp.Text);
                    Console.WriteLine("Connected and waiting...");
                    Console.WriteLine(" *** Press <key> to exit.");
                    Console.ReadKey();
                }

                client.Stop();
                client.Dispose();
                client = null;

                Console.WriteLine("Stopped.");
            }
            else
            {
                Console.WriteLine("Could not connect.");
                Console.WriteLine("Press <enter> to exit.");
                Console.ReadLine();
            }

            if (doSelfServer)
            {
                server.Stop();
                server.Dispose();
                server = null;
            }
        }

        private static void Clinet_MessageRecived(object sender, WebsocketPipe.WebsocketPipe<CSCom.NPMessage>.MessageEventArgs e)
        {
            Task.Run(() =>
            {
                if (e.Message.MessageType == CSCom.NPMessageType.Error)
                {
                    Console.WriteLine("************************\nError recived from client:\n" + e.Message.Text);
                    return;
                }

                Console.WriteLine("Client recived " + (e.Message.Text == null ? "Empty message." : "\"" + e.Message.Text + "\""));
            });

        }

        private static void Server_MessageRecived1(object sender, WebsocketPipe.WebsocketPipe<CSCom.NPMessage>.MessageEventArgs e)
        {
            Task.Run(() =>
            {
                if (e.RequiresResponse)
                {
                    e.Response = new NPMessage(NPMessageType.Warning, null, "kka");
                }

                if (e.Message.MessageType == CSCom.NPMessageType.Error)
                {
                    Console.WriteLine("************************\nError recived from client:\n" + e.Message.Text);
                    return;
                }

                Console.WriteLine("Recived map with " + e.Message.NamepathsCount + " name paths.");
            });

            if (WaitAtServer > 0)
                System.Threading.Thread.Sleep(WaitAtServer);
        }
    }
}
