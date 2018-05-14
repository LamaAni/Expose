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
        static void Main(string[] args)
        {
            bool doSelfServer = true;
            CSCom.CSCom server = null;
            if (doSelfServer)
            {
                server = new CSCom.CSCom();
                server.Log+=(s,e)=>{
                    Console.WriteLine(e.Message);
                };
                server.Listen();
                server.MessageRecived += Server_MessageRecived1; ;
            }


            bool waitBeforeStopping = true;
            CSCom.CSCom clinet = new CSCom.CSCom();
            clinet.Log += (s, e) => {
                Console.WriteLine(e.Message);
            };
            clinet.Connect();
            clinet.MessageRecived += Clinet_MessageRecived;
            System.Threading.Thread.Sleep(100);
            if(clinet.IsAlive)
            {
                Console.WriteLine("Connected to server.");
                CSCom.NPMessageNamepathData data = new CSCom.NPMessageNamepathData();
                float[] valToSend = new float[10000];
                valToSend[9999] = 23;
                data.Value = valToSend;
                data.Namepath = "lama";
                Stopwatch watch = new Stopwatch();
                watch.Start();
                NPMessage rsp= clinet.Send(CSCom.NPMessageType.Warning, "lama", (CSCom.NPMessageNamepathData)null, true); ;
                watch.Stop();
                if (waitBeforeStopping || doSelfServer)
                {
                    Console.WriteLine("Waited for send[ms]: " + watch.Elapsed.TotalMilliseconds);
                    if (rsp != null)
                        Console.WriteLine("Recived response text: " + rsp.Text);
                    Console.WriteLine("Connected and waiting...");
                    Console.WriteLine("Press <enter> to exit.");
                    Console.ReadLine();
                }

                clinet.Stop();
                clinet.Dispose();
                clinet = null;


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
            if (e.Message.MessageType == CSCom.NPMessageType.Error)
            {
                Console.WriteLine("************************\nError recived from client:\n" + e.Message.Text);
                return;
            }

            Console.WriteLine("Client recived " + (e.Message.Text == null ? "Empty message." : "\"" + e.Message.Text + "\""));
        }

        private static void Server_MessageRecived1(object sender, WebsocketPipe.WebsocketPipe<CSCom.NPMessage>.MessageEventArgs e)
        {
            e.Response = new NPMessage(NPMessageType.Warning, null, "kka");
            if(e.Message.MessageType== CSCom.NPMessageType.Error)
            {
                Console.WriteLine("************************\nError recived from client:\n" + e.Message.Text);
                return;
            }
            CSCom.CSCom server = (CSCom.CSCom)sender;
            Console.WriteLine("Recived map with " + e.Message.Namepaths.Count + " name paths");
        }
    }
}
