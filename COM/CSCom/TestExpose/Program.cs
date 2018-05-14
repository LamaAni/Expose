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
        static EasyConsole.Menu menu;
        static void Main(string[] args)
        {
            // starting the menu.
            bool continuteToNext = true;
            menu = new EasyConsole.Menu()
                .Add("Exit", () => { continuteToNext = false; })
                .Add("Send warning command to client.", () =>
                 {
                     Console.WriteLine("Write something to log <enter for default log>:");
                     string theWarning = Console.ReadLine();
                     if (theWarning == null || theWarning.Length == 0)
                         theWarning = "[Somekinda default warning...]";

                     NPMessage rsp = Client.Send(NPMessageType.Warning,
                         theWarning, (NPMessageNamepathData)null);

                     if (rsp == null)
                         return;

                     Console.WriteLine("Expose has responded with: " + rsp.Text == null ? "[UNKNOWN!]" : rsp.Text);
                 })
                 .Add("Test get response: ",()=>
                 {
                     Console.WriteLine("Waht is the property to get? [<enter> = 'TestString']");
                     string theprop = Console.ReadLine();
                     if (theprop.Length == 0)
                         theprop = "TestString,TestMatrix";

                     string[] props = theprop.Split(',').Select(p => p.Trim()).ToArray();

                     NPMessage rsp = Client.Get(props);

                     // printing the information gathered.
                     if(rsp==null || rsp.NamePaths==null)
                     {
                         Console.WriteLine("Property '" + theprop + "' not found.");
                     }
                     else
                     {
                         Console.WriteLine("Got Property '" + theprop + "':");
                         foreach (var npd in rsp.NamePaths)
                         {
                             Console.WriteLine("\t" + npd.Namepath + "(" + npd.Value.GetType() + "): " + npd.Value);
                         }
                     }
                 });

            Client.Connect();
            Client.MessageRecived += Client_MessageRecived;
            Client.Log += Client_Log;

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
            Console.WriteLine();
            switch(e.Message.MessageType)
            {
                case NPMessageType.Warning:
                    Console.WriteLine("Client Warning: " + e.Message.Text);
                    break;
                case NPMessageType.Error:
                    Console.WriteLine("************************\nError recived from client:\n" + e.Message.Text);
                    break;
                default:
                    Console.WriteLine("Client unhandled "+e.Message.MessageType+" msg: " + (e.Message.Text == null ? "Empty message." : "\"" + e.Message.Text + "\""));
                    break;
            }
        }
    }
}
