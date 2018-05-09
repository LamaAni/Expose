using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tester
{
    class Program
    {
        static void Main(string[] args)
        {
            bool doSelfServer = false;
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
            CSCom.CSCom clinet = new CSCom.CSCom();
            clinet.Log += (s, e) => {
                Console.WriteLine(e.Message);
            };
            clinet.Connect();
            System.Threading.Thread.Sleep(100);
            if(clinet.IsAlive)
            {
                Console.WriteLine("Connected to server.");
                CSCom.NPMessageNamepathData data = new CSCom.NPMessageNamepathData();
                float[] valToSend = new float[10000];
                valToSend[9999] = 23;
                data.Value = valToSend;
                data.Namepath = "lama";
                clinet.Send(new CSCom.NPMessage(CSCom.NPMessageType.AsString, new CSCom.NPMessageNamepathData[] { data }, "test message"));
                System.Threading.Thread.Sleep(100);

                Console.WriteLine("Press <enter> to exit.");

                if(doSelfServer)
                    Console.ReadLine();
                
                clinet.Stop();
                clinet.Dispose();
                clinet = null;
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

        private static void Server_MessageRecived1(object sender, WebsocketPipe.WebsocketPipe<CSCom.NPMessage>.MessageEventArgs e)
        {
            CSCom.CSCom server = (CSCom.CSCom)sender;
            Console.WriteLine("Recived map with " + e.Message.Namepaths.Count + " name paths");
        }
    }
}
