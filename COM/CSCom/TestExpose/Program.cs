using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CSCom;

namespace TestExpose
{
    class Program
    {
        static CSCom.CSCom Client = new CSCom.CSCom();
        static void Main(string[] args)
        {
            // connecting to client.
            Client.Connect();
            Client.MessageRecived += Client_MessageRecived;
            Client.Log += Client_Log;

            // starting the menu.
            bool continuteToNext = true;
            var menu = new EasyConsole.Menu()
                .Add("Exit", () => { continuteToNext = false; })
                .Add("Send log command to client.", () =>
                 {
                     Console.WriteLine("Write something to log <enter for default log>:");
                     string log = Console.ReadLine();
                     if (log == null || log.Length == 0)
                         log = "[Kabba!!]";

                     NPMessage rsp = Client.Send(NPMessageType.Data, "log",
                         new NPMessageNamepathData()
                         {
                             Value = log,
                             Namepath = ""
                         }, true);

                     if (rsp == null)
                         return;

                     Console.WriteLine("Expose has responded with: " + rsp.Message == null ? "[UNKNOWN!]" : rsp.Message);
                 });

            
            while (continuteToNext)
            {
                menu.Display();
                if (!continuteToNext)
                    break;
                
                //Console.WriteLine("Press q to exit or any other key to continue.");
                //continuteToNext = Console.ReadKey(true).Key != ConsoleKey.Q;
            }
        }

        private static void Client_Log(object sender, CSCom.CSCom.LogEventArgs e)
        {
            Console.WriteLine(e.Message);
        }

        private static void Client_MessageRecived(object sender, WebsocketPipe.WebsocketPipe<NPMessage>.MessageEventArgs e)
        {
            if (e.Message.MessageType == CSCom.NPMessageType.Error)
            {
                Console.WriteLine("************************\nError recived from client:\n" + e.Message.Message);
                return;
            }

            Console.WriteLine("Client recived " + (e.Message.Message == null ? "Empty message." : "\"" + e.Message.Message + "\""));
        }
    }
}
