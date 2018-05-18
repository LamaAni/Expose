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
        static CSCom.CSCom Com = new CSCom.CSCom();
        static EasyConsole.Menu menu;
        static bool SilentSetMode = false;
        static int SilentSetModeCount = 0;

        static void Main(string[] args)
        {
            // starting the menu.
            bool continuteToNext = true;
            menu = new EasyConsole.Menu()
                .Add("Exit", () => { continuteToNext = false; })
                .Add("Connect", () =>
                {
                    Com.Connect();
                    Com.Pipe.WS.OnError += WS_OnError;
                    Console.WriteLine("Connected to remote.");
                })
                .Add("Listen", () =>
                {
                    Com.Listen();

                    Console.WriteLine("Listening for connections.");
                })
                .Add("Flip logging", () =>
                {
                    Com.DoLogging = !Com.DoLogging;

                    Console.WriteLine("Logging is " + (Com.DoLogging ? "ON" : "OFF"));
                })
                .Add("Send warning command to client.", () =>
                 {
                     Console.WriteLine("Write something to log <enter for default log>:");
                     string theWarning = Console.ReadLine();
                     if (theWarning == null || theWarning.Length == 0)
                         theWarning = "[Somekinda default warning...]";

                     NPMessage rsp = Com.Send(NPMessageType.Warning,
                         theWarning, (NPMessageNamepathData)null);

                     if (rsp == null)
                         return;

                     Console.WriteLine("Expose has responded with: " + rsp.Text == null ? "[UNKNOWN!]" : rsp.Text);
                 })
                 .Add("Send simple set", () =>
                 {
                     Console.WriteLine("Waht is the property to set (string)? [<enter> = 'TestString']");
                     string theprop = Console.ReadLine();
                     Console.WriteLine("Waht is the value to set (string)? [<enter> = 'Lama?']");
                     string thevalue = Console.ReadLine();

                     Com.Send(NPMessageType.Set, "", new NPMessageNamepathData(
                         theprop == "" ? "TestString" : theprop,
                         thevalue == "" ? "Lama?" : thevalue));
                 })
                 .Add("Send test matrix", () =>
                 {
                     Console.WriteLine("Waht is the property to set (string)? [<enter> = 'TestMatrix']");
                     string theprop = Console.ReadLine();
                     Console.WriteLine("MatirixSize a*a, a= ? [<enter> = '1000']");
                     string thevalue = Console.ReadLine();

                     int a = int.TryParse(thevalue, out a) ? a : 1000;
                     theprop = theprop == "" ? "TestMatrix" : theprop;
                     Array mat = MakeLargeNumericMatrix<double>(a);
                     Com.Send(NPMessageType.Set, "", new NPMessageNamepathData(theprop, mat),true);
                     Console.WriteLine("Updating " + theprop + " with matrix of length " + mat.Length);
                 })
                 .Add("Send repeating large matrix until stopped.", () =>
                 {
                     Console.WriteLine("Waht is the property to set (string)? [<enter> = 'TestMatrix']");
                     string theprop = Console.ReadLine();
                     Console.WriteLine("MatirixSize a*a, a= ? [<enter> = '1000']");
                     string thevalue = Console.ReadLine();

                     int a = int.TryParse(thevalue, out a) ? a : 1000;
                     theprop = theprop == "" ? "TestMatrix" : theprop;
                    
                     Console.WriteLine("Starting continues updates of " + theprop + " with matrix of length " + a*a);
                     bool isRunning = true;
                     Task.Run(() =>
                     {
                         while (isRunning)
                         {
                             Array mat = MakeLargeNumericMatrix<double>(a);
                             Com.Send(NPMessageType.Set, "", new NPMessageNamepathData(theprop, mat), true);
                             Console.WriteLine("Send success.");
                         }
                     });

                     Console.WriteLine("Any key to stop.");
                     Console.ReadKey();
                     isRunning = false;
                 })
                 .Add("Flip set silent mode",()=>
                 {
                     SilentSetMode = !SilentSetMode;
                     SilentSetModeCount = 0;
                     Console.WriteLine("Silent set moe: " + SilentSetMode);
                     
                 })
                 .Add("Test get response: ",()=>
                 {
                     Console.WriteLine("Waht is the property to get? [<enter> = 'TestString']");
                     string theprop = Console.ReadLine();
                     if (theprop.Length == 0)
                         theprop = "TestString,TestMatrix";

                     string[] props = theprop.Split(',').Select(p => p.Trim()).ToArray();

                     NPMessage rsp = Com.Get(props);

                     // printing the information gathered.
                     if(rsp == null || rsp.NamePaths==null)
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

            
            
            Com.MessageRecived += Client_MessageRecived;
            Com.Log += Client_Log;

            while (continuteToNext)
            {
                menu.Display();
                if (!continuteToNext)
                    break;
                
                //Console.WriteLine("Press q to exit or any other key to continue.");
                //continuteToNext = Console.ReadKey(true).Key != ConsoleKey.Q;
            }

            Console.WriteLine("Any key to exit...");
            Console.ReadKey();
        }

        private static T[,] MakeLargeNumericMatrix<T>(int a)
        {
            T[,] mat = new T[a, a];
            Random r = new Random();
            for (int i = 0; i < a; i++)
                for (int j = 0; j < a; j++)
                    mat[i, j] = (T)Convert.ChangeType(r.NextDouble(), typeof(T));
            return mat;
        }

        private static void WS_OnError(object sender, WebSocketSharp.ErrorEventArgs e)
        {
            Console.WriteLine(e.Message);
            throw e.Exception;
        }

        private static void Client_Log(object sender, CSCom.CSCom.LogEventArgs e)
        {
            Console.WriteLine(e.Message);
        }

        private static void Client_MessageRecived(object sender, WebsocketPipe.WebsocketPipe<NPMessage>.MessageEventArgs e)
        {

            if (e.Message == null)
            {
                Console.WriteLine("Recived null message from server.");
                return;
            }

            if (SilentSetMode && e.Message.MessageType == NPMessageType.Set)
            {
                SilentSetModeCount++;
                return;
            }

            Console.WriteLine();
            Console.WriteLine("Recived message from " + (Com.IsListening ? "client" : "server") + " of type: " + e.Message.MessageType + (e.RequiresResponse ? "(Requires response)" : ""));

            switch(e.Message.MessageType)
            {
                case NPMessageType.Warning:
                    Console.WriteLine("Client Warning: " + e.Message.Text);
                    break;
                case NPMessageType.Error:
                    Console.WriteLine("************************\nError recived from client:\n" + e.Message.Text);
                    break;
                case NPMessageType.Invoke:
                    if (e.Message.Text == "GetSilentModeCount")
                    {
                        Console.WriteLine("Mode count: " + SilentSetModeCount);
                        e.Response = NPMessage.FromValue(SilentSetModeCount);
                    }
                    Console.WriteLine("Unhandled invoke: \n" + e.Message);
                    break;
                default:
                    Console.WriteLine("Unhandled msg: \n" + e.Message);
                    
                    break;
            }
            Console.WriteLine();
            Console.WriteLine("Select: ");
        }
    }
}
