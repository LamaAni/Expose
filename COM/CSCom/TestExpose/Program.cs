using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        static int WaitOnMessageRecived = 400;

        [STAThread]
        static void Main(string[] args)
        {
            // starting the menu.
            bool continuteToNext = true;
            menu = new EasyConsole.Menu()
                .Add("Exit", () => { continuteToNext = false; })
                .Add("Connect", () =>
                {
                    Com.Connect();
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
                     Com.Send(NPMessageType.Set, "", new NPMessageNamepathData(theprop, mat), true);
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

                     Console.WriteLine("Starting continues updates of " + theprop + " with matrix of length " + a * a);
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
                 .Add("Send Bing and small", () =>
                 {
                     Console.WriteLine("Sending big and small data to check data sync.");
                     Console.WriteLine("Send big (no need for response).");
                     Com.Send(NPMessage.FromValue(new double[10000], NPMessageType.Set, "big"));
                     Console.WriteLine("Send small (need for response).");
                     Com.Send(NPMessage.FromValue("", NPMessageType.Invoke, "small"), true);
                 })
                 .Add("Send fast large matrix to update, (Fail on labview?)", () =>
                 {
                     Console.WriteLine("Waht is the property to set (string)? [<enter> = 'TestMatrix']");
                     string theprop = Console.ReadLine();
                     Console.WriteLine("Delay between sends [ms] ? [<enter> = '100']");
                     string thevalue = Console.ReadLine();
                     Console.WriteLine("# of messages to send ? [<enter> = '100']");
                     string thecount = Console.ReadLine();

                     int dt = int.TryParse(thevalue, out dt) ? dt : 100;
                     int n = int.TryParse(thecount, out n) ? n : 100;
                     bool isRunning = true;
                     int sentCount = 0;
                     Task.Run(() =>
                     {
                         while (isRunning)
                         {
                             System.Threading.Thread.Sleep(dt);
                             Array mat = MakeLargeNumericMatrix<double>(1000);
                             Com.Send(NPMessageType.Set, "", new NPMessageNamepathData(theprop, mat), false);
                             sentCount++;
                             if (sentCount > n)
                             {
                                 Console.WriteLine("Completed send of " + sentCount + " messages");
                                 isRunning = false;
                             }
                         }
                     });

                     Console.WriteLine("Any key to stop.");
                     Console.ReadKey();
                     isRunning = false;
                     Console.WriteLine("Sent " + sentCount + " messages.");
                 })
                 .Add("Flip set silent mode", () =>
                  {
                      SilentSetMode = !SilentSetMode;
                      SilentSetModeCount = 0;
                      Console.WriteLine("Silent set moe: " + SilentSetMode);
                  })
                 .Add("Test get response: ", () =>
                  {
                      Console.WriteLine("Waht is the property to get? [<enter> = 'TestString']");
                      string theprop = Console.ReadLine();
                      if (theprop.Length == 0)
                          theprop = "TestString,TestMatrix";

                      string[] props = theprop.Split(',').Select(p => p.Trim()).ToArray();

                      NPMessage rsp = Com.Get(props);

                      // printing the information gathered.
                      if (rsp == null || rsp.NamePaths == null)
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
                  })
                  .Add("Test multiple connection clients",()=>
                  {
                      var clientA = new CSCom.CSCom();
                      var clientB = new CSCom.CSCom();
                      clientA.Connect();
                      Console.WriteLine("Connected client A.");
                      clientB.Connect();
                      Console.WriteLine("Connected client B.");
                      clientA.Send(NPMessage.FromValue(new byte[200]),true);
                      Console.WriteLine("Send client a ok.");
                      clientB.Send(NPMessage.FromValue(new byte[200]),true);
                      Console.WriteLine("Send client b ok.");

                      Console.WriteLine("Press any key to disconnect...");
                      Console.ReadKey();

                      clientA.Stop();
                      clientB.Stop();
                  })
                 .Add("Test websocket client server", () =>
                 {
                     Console.WriteLine("Client server test");
                     WebsocketPipe.WebsocketPipeWS server = new WebsocketPipe.WebsocketPipeWS(new Uri("ws://localhost:50001/CSCom"));
                     WebsocketPipe.WebsocketPipeWS client = new WebsocketPipe.WebsocketPipeWS(new Uri("ws://localhost:50001/CSCom"));
                     int smsgIdx = 0, cmsgIdx = 0;
                     Action<string, int, byte[]> pnt = (caller, idx, data) =>
                       {
                           Console.WriteLine(caller + " recived msg (#" + idx + "), with " + (data == null ? 0 : data.Length) + " bytes");
                       };
                     server.MessageRecived += (s, e) =>
                     {
                         pnt("server", smsgIdx, e.Data);
                         smsgIdx++;
                     };
                     client.MessageRecived += (s, e) =>
                     {
                         pnt("client", cmsgIdx, e.Data);
                         cmsgIdx++;
                     };
                     server.Listen();
                     client.Connect();
                     byte[] bigdata = new byte[10000];
                     byte[] smalldata = new byte[1];
                     Console.WriteLine("Connected.");
                     Console.WriteLine("Sending from client:");
                     int n = 5;
                     for (int i = 0; i < n; i++)
                     {
                         client.Send(bigdata);
                         client.Send(smalldata);
                     }
                     System.Threading.Thread.Sleep(1000);
                     Console.WriteLine("Sending from server:");
                     for (int i = 0; i < n; i++)
                     {
                         server.Send(bigdata);
                         server.Send(smalldata);
                     }

                     System.Threading.Thread.Sleep(1000);
                     client.Stop();
                     server.Stop();
                 })
                 .Add("Test websocket pipe client server", () =>
                 {
                     Console.WriteLine("Client server test");
                     WebsocketPipe.WebsocketPipe<byte[]> server = new WebsocketPipe.WebsocketPipe<byte[]>(new Uri("ws://localhost:50001/CSCom"));
                     WebsocketPipe.WebsocketPipe<byte[]> client = new WebsocketPipe.WebsocketPipe<byte[]>(new Uri("ws://localhost:50001/CSCom"));
                     server.Timeout = 1000;
                     client.Timeout = 1000;

                     int smsgIdx = 0, cmsgIdx = 0;
                     object loglock = new object();
                     Action<string, int, byte[], int> pnt = (caller, idx, data, inblock) =>
                     {

                             Console.WriteLine(caller + " recived msg (#" + idx + "), with " + (data == null ? 0 : data.Length) + " bytes, " + inblock + " in block");
                     };
                     server.MessageRecived += (s, e) =>
                     {
                         lock (loglock)
                         {
                             pnt("server", smsgIdx, e.Message, e.NumberOfMessagesInBlock);
                             smsgIdx++;
                         }
                     };
                     client.MessageRecived += (s, e) =>
                     {
                         lock (loglock)
                         {
                             pnt("client", cmsgIdx, e.Message, e.NumberOfMessagesInBlock);
                             cmsgIdx++;
                         }
                     };
                     server.Listen();
                     System.Threading.Thread.Sleep(10);
                     client.Connect();
                     System.Threading.Thread.Sleep(10);
                     byte[] bigdata = new byte[1000000];
                     byte[] smalldata = new byte[1];
                     Console.WriteLine("Connected.");
                     Console.WriteLine("Sending from client:");
                     int n = 5;
                     for (int i = 0; i < n; i++)
                     {
                         client.Send(bigdata);
                         client.Send(smalldata);
                     }
                     Console.WriteLine("Completed sending, waiting for recive.");
                     while (smsgIdx < n)
                     {
                         System.Threading.Thread.Sleep(10);
                     }
                     System.Threading.Thread.Sleep(10);
                     Console.WriteLine("Sending from server:");
                     for (int i = 0; i < n; i++)
                     {
                         server.Send(bigdata);
                         server.Send(smalldata);
                     }
                     Console.WriteLine("Completed sending, waiting for recive.");
                     while (cmsgIdx < n)
                     {
                         System.Threading.Thread.Sleep(10);
                     }
                     System.Threading.Thread.Sleep(10);
                     Console.WriteLine("Complete. Closing.");
                     client.Stop();
                     server.Stop();

                     client.Dispose();
                     server.Dispose();
                 })
                 .Add("Test memory mapped file stack", () =>
                 {
                     WebsocketPipe.MemoryMappedBinaryStack stack = new WebsocketPipe.MemoryMappedBinaryStack("Lama this stack");
                     List<byte[]> els = new List<byte[]>();
                     int n = 100;
                     Random rn = new Random();
                     int totalSize = 0;
                     Console.WriteLine("Writing arrays of sizes:");
                     for (int i = 0; i < n; i++)
                     {
                         byte[] ar = new byte[rn.Next(1000000)];
                         totalSize += ar.Length;
                         els.Add(ar);
                         Console.Write((i == 0 ? "" : ", ") + ar.Length);
                     }
                     Console.WriteLine();

                     Stopwatch watch = new Stopwatch();
                     watch.Start();
                     //stack.Lock();
                     foreach (byte[] ar in els)
                         stack.Push(ar);
                     //stack.UnLock();
                     watch.Stop();
                     Console.WriteLine();
                     Console.WriteLine("Pushed total of " + totalSize + " [bytes] in " + els.Count + " elements in [ms]: " + watch.Elapsed.TotalMilliseconds);
                     Console.WriteLine("Reading...");
                     watch.Reset();
                     watch.Start();
                     //stack.Lock();
                     for (int i = 0; i < els.Count; i++)
                     {
                         byte[] read = stack.Pop();
                         byte[] shouldBe = els[els.Count - i - 1];
                         Console.WriteLine("read " + read.Length + " ?= " + shouldBe.Length + " " + (read.Length == shouldBe.Length ? "OK" : "ERROR"));
                     }
                     //stack.UnLock();
                     
                     Console.WriteLine("Read total of " + totalSize + " [bytes] in " + els.Count + " elements in [ms]: " + watch.Elapsed.TotalMilliseconds);

                     watch.Reset();
                     watch.Start();
                     stack.Push(els.ToArray());
                     watch.Stop();
                     Console.WriteLine("Multipush same array in [ms] " + watch.Elapsed.TotalMilliseconds);

                     watch.Reset();
                     watch.Start();
                     byte[][] readEls = stack.Empty().Reverse().ToArray();
                     watch.Stop();
                     Console.WriteLine("Multipop (empty) same array in [ms] " + watch.Elapsed.TotalMilliseconds);
                     int errored = 0;
                     for (int i = 0; i < els.Count; i++)
                     {
                         byte[] read = readEls[i];
                         byte[] shouldBe = els[i];
                         if (read.Length != shouldBe.Length)
                         {
                             Console.WriteLine("read " + read.Length + " ?= " + shouldBe.Length + " -> ERROR");
                             errored++;
                         }
                     }
                     Console.WriteLine(errored+ " errors found.");

                     stack.Dispose();
                 })
                 .Add("Test memory mapped file queue", () =>
                 {
                     WebsocketPipe.MemoryMappedBinaryQueue queue = new WebsocketPipe.MemoryMappedBinaryQueue("Lama this stack");
                     List<byte[]> els = new List<byte[]>();
                     int n = 100;
                     Random rn = new Random();
                     int totalSize = 0;
                     Console.WriteLine("Writing arrays of sizes:");
                     for (int i = 0; i < n; i++)
                     {
                         byte[] ar = new byte[rn.Next(1000000)];
                         ar[0] = 1;
                         totalSize += ar.Length;
                         els.Add(ar);
                         Console.Write((i == 0 ? "" : ", ") + ar.Length);
                     }
                     Console.WriteLine();

                     Stopwatch watch = new Stopwatch();
                     watch.Start();
                     //stack.Lock();
                     foreach (byte[] ar in els)
                         queue.Enqueue(ar);
                     //stack.UnLock();
                     watch.Stop();
                     Console.WriteLine();
                     Console.WriteLine("Pushed total of " + totalSize + " [bytes] in " + els.Count + " elements in [ms]: " + watch.Elapsed.TotalMilliseconds);
                     Console.WriteLine("Reading...");
                     watch.Reset();
                     watch.Start();
                     //stack.Lock();
                     for (int i = 0; i < els.Count; i++)
                     {
                         byte[] read = queue.Dequeue();
                         byte[] shouldBe = els[i];
                         Console.WriteLine("read " + read.Length + " ?= " + shouldBe.Length + " " + (read.Length == shouldBe.Length ? "OK" : "ERROR"));
                     }
                     //stack.UnLock();

                     Console.WriteLine("Read total of " + totalSize + " [bytes] in " + els.Count + " elements in [ms]: " + watch.Elapsed.TotalMilliseconds);

                     watch.Reset();
                     watch.Start();
                     queue.Enqueue(els.ToArray());
                     watch.Stop();
                     Console.WriteLine("Multipush same array in [ms] " + watch.Elapsed.TotalMilliseconds);

                     watch.Reset();
                     watch.Start();
                     byte[][] readEls = queue.Empty().ToArray();
                     watch.Stop();
                     Console.WriteLine("Multipop (empty) same array in [ms] " + watch.Elapsed.TotalMilliseconds);
                     int errored = 0;
                     for (int i = 0; i < els.Count; i++)
                     {
                         byte[] read = readEls[i];
                         byte[] shouldBe = els[i];
                         if (read.Length != shouldBe.Length)
                         {
                             Console.WriteLine("read " + read.Length + " ?= " + shouldBe.Length + " -> ERROR");
                             errored++;
                         }
                     }
                     Console.WriteLine(errored + " errors found.");

                     queue.Dispose();
                 })
                 .Add("Open matlab file",()=>
                 {
                     System.Windows.Forms.OpenFileDialog ofd = new System.Windows.Forms.OpenFileDialog();
                     ofd.Filter = "Matlab Files|*.m";
                     if(ofd.ShowDialog()== System.Windows.Forms.DialogResult.OK)
                     {
                         CSCom.CSCom.OpenMatlabFile(ofd.FileName);
                     }
                     
                 })
                 .Add("Test event dispatch",()=> {
                     var last = DateTime.Now;
                     int delay = 1000;
                     DelayedEventDispatch dispatch = new DelayedEventDispatch();
                     dispatch.Ready += (s, e) =>
                     {
                         DateTime now = DateTime.Now;
                         if ((now - last).TotalMilliseconds < delay)
                             Console.WriteLine("Dispatch error.");
                         else Console.WriteLine("Dispatch");
                         last = now;
                     };

                     for(int i=0;i<10000;i++)
                     {
                         dispatch.Trigger(delay);
                         if (i % 2==0)
                             System.Threading.Thread.Sleep(1);
                     }

                     Console.WriteLine("ok");
                 });



            Com.MessageRecived += On_MessageRecived;
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

        private static void Client_Log(object sender, CSCom.CSCom.LogEventArgs e)
        {
            Console.WriteLine(e.Message);
        }

        static Task m_writeSelect = null;
        static DateTime m_writeSelectAt = new DateTime();
        private static void On_MessageRecived(object sender, WebsocketPipe.WebsocketPipe<NPMessage>.MessageEventArgs e)
        {
            if (WaitOnMessageRecived > 0)
                System.Threading.Thread.Sleep(WaitOnMessageRecived);

            if (e.Message == null)
            {
                Console.WriteLine("Recived null message from server.");
                return;
            }

            if (SilentSetMode && e.Message.MessageType == NPMessageType.Set)
            {
                SilentSetModeCount++;
                Console.WriteLine("Recived " + SilentSetModeCount);
                return;
            }

            Console.WriteLine();
            Console.WriteLine("Recived message from " +
                e.WebsocketID + (Com.IsListening ? " (client)" : " (server)") +
                ": " + e.Message.MessageType + (e.RequiresResponse ? "(Requires response)" : "") +
                (e.NumberOfMessagesInBlock > 1 ? " (BN:" + e.NumberOfMessagesInBlock + ")" : ""));

            switch(e.Message.MessageType)
            {
                case NPMessageType.Warning:
                    Console.WriteLine("Client Warning: " + e.Message.Text);
                    break;
                case NPMessageType.Error:
                    Console.WriteLine("************************\nError recived from client:\n" + e.Message.Text);
                    break;
                case NPMessageType.Create:
                case NPMessageType.Destroy:
                    break;
                case NPMessageType.Invoke:
                    switch (e.Message.Text)
                    {
                        case "GetSilentModeCount":
                            {
                                Console.WriteLine("Mode count: " + SilentSetModeCount);
                                e.Response = NPMessage.FromValue(SilentSetModeCount);
                            }
                            break;
                        case "static.OpenExperiment":
                            e.Response = NPMessage.FromValue("dummyid");
                            break;
                        default:
                            Console.WriteLine("Unknown function called, " + e.Message.Text);
                            break;
                    }
                    Console.WriteLine("Unhandled invoke: \n" + e.Message);
                    break;
                default:
                    Console.WriteLine("Unhandled msg: \n" + e.Message);
                    
                    break;
            }
            m_writeSelectAt = DateTime.Now + TimeSpan.FromSeconds(1);
            if (m_writeSelect == null)
            {
                m_writeSelect = new Task(() =>
                  {
                      while (m_writeSelectAt > DateTime.Now)
                          System.Threading.Thread.Sleep(10);
                      Console.WriteLine("Select: ");
                      m_writeSelect = null;
                  });
                m_writeSelect.Start();
            }
            //Console.WriteLine();
            
        }
    }
}
