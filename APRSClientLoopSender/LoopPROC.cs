//
// C#
// dkxce APRS Client Loop Sender
// v 0.2, 03.06.2024
// https://github.com/dkxce/APRSClientLoopSender
// en,ru,1251,utf-8
//

using System;
using System.Collections.Generic;
using System.Threading;
using System.Xml;
using System.Windows.Forms;
using System.ServiceProcess;
using System.IO;

namespace APRSClientLoopSender
{
    internal class LoopPROC
    {
        internal static string     ApplicationCaption { private set; get; } = "dkxce APRS Client Loop Sender";
        internal static string     ApplicationVersion { private set; get; } = "0.2";
        internal static string     ApplicationTitle   { private set; get; } = $"{ApplicationCaption} v{ApplicationVersion.PadRight(6)} ";
        internal static string     ApplicationWebSite { private set; get; } = "https://github.com/dkxce/APRSClientLoopSender";
        internal static XMLConfig  ApplicationConfig  { private set; get; } = null;
        internal static string     AppConfigFile      { private set; get; } = "APRSClientLoopSender.xml";
        internal static string     ApplicationLogFile { private set; get; } = Path.Combine(XMLSaved<int>.CurrentDirectory(), "log.txt");

        #region INTERNAL METHODS

        internal static void ClearLog()
        {
            try { if (File.Exists(ApplicationLogFile)) File.Delete(ApplicationLogFile); } catch { };
        }

        internal static void WriteConsole(string msg, bool withDT = false, ConsoleColor? color = null, ConsoleColor? firstLineColor = null)
        {
            WriteConsole(new string[] { msg }, withDT, color, firstLineColor);
        }

        internal static void WriteConsole(string[] msg, bool withDT = false, ConsoleColor? color = null, ConsoleColor? firstLineColor = null)
        {
            ConsoleColor cc = Console.ForegroundColor;
            if(color.HasValue || firstLineColor.HasValue) Console.ForegroundColor = ConsoleColor.White;
            if (withDT) Console.Write($"{DateTime.Now}: ");            
            for (int msg_id = 0; msg_id < msg.Length; msg_id++)
            {
                if(color.HasValue && ((msg_id == 0 && msg.Length == 1 ) || (msg_id == 1))) Console.ForegroundColor = color.Value;
                if(firstLineColor.HasValue && msg_id == 0) Console.ForegroundColor = firstLineColor.Value;
                Console.WriteLine(msg[msg_id]);
            };
            if (color.HasValue || firstLineColor.HasValue) Console.ForegroundColor = cc;

            try 
            {
                string lockFile = Path.Combine(XMLSaved<ArrangeStartingPosition>.CurrentDirectory(), "log.txt.lock");
                if (!File.Exists(lockFile)) try { File.Delete(ApplicationLogFile); } catch { };
                File.WriteAllText(lockFile, $"{DateTime.UtcNow}");
            } 
            catch { };

            try
            {
                using (FileStream fs = new FileStream(ApplicationLogFile, FileMode.Append, FileAccess.Write))
                using (StreamWriter sw = new StreamWriter(fs, System.Text.Encoding.GetEncoding(1251)))
                {
                    fs.Position = fs.Length;
                    if (withDT) sw.Write($"{DateTime.Now}: ");
                    foreach (string m in msg) sw.WriteLine(m);
                };
            }
            catch { };
        }

        internal static void Exit(int code)
        {
            string txt = "";
            switch (code)
            {
                case 0:  txt = " Run as Console Successfull"; break;
                case 1:  txt = " Run Failed"; break;
                case 2:  txt = " Help Page Successfull"; break;
                case 3:  txt = " Run as Service Successfull"; break;
                case 4:  txt = " Install Service Successfull"; break;
                case 5:  txt = " Uninstall Service Successfull"; break;
                case 6:  txt = " Start Service Successfull"; break;
                case 7:  txt = " Stop Service Successfull"; break;
                case 8:  txt = " Run as Administrator Failed"; break;
                case 9:  txt = " Start Service Failed"; break;
                case 10: txt = " Stop Service Failed"; break;
                case 11: txt = " Run as Service Failed"; break;
            };           
            WriteConsole($"\r\nExit code: {code}{txt}");

            try { File.Delete(Path.Combine(XMLSaved<ArrangeStartingPosition>.CurrentDirectory(), "log.txt.lock")); } catch { };

            Thread.Sleep(2000);
            Environment.Exit(code);
        }

        #endregion INTERNAL METHODS

        #region ENTRY POINT

        static int Main(string[] args)
        {
            //bool entryPointForDebugger = true;
            //while (entryPointForDebugger)
            //    Thread.Sleep(1000);

            // Catch Exceptions
            {
                Application.ThreadException += Application_ThreadException;
                AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
            };

            // Set Console Title
            if (Environment.UserInteractive) try { Console.Title = ApplicationTitle.Trim(); } catch { };

            // Get Config XML Specified by File
            {
                if (!System.IO.File.Exists(System.IO.Path.Combine(XMLSaved<int>.CurrentDirectory(), AppConfigFile))) XMLConfig.SaveTemplate();
                foreach (string a in args) if (!a.StartsWith("/")) { try { if (System.IO.File.Exists(a)) AppConfigFile = a; } catch { }; };
            };

            // WRITE HEADER
            {
                WriteConsole("*********************************************\r\n*                                           *");
                WriteConsole($"*    {ApplicationTitle} *");
                WriteConsole("*                                           *\r\n*********************************************");
                WriteConsole("https://github.com/dkxce/APRSClientLoopSender");
                WriteConsole("");
            };            

            // READ CONFIG
            {
                WriteConsole(String.Format("Configuration: {0}", System.IO.Path.GetFileName(AppConfigFile)), false, ConsoleColor.Yellow);
                WriteConsole("");
                bool error = false;
                try { ApplicationConfig = XMLConfig.LoadNormal(AppConfigFile); }
                catch (Exception ex)
                {
                    WriteConsole($"No Config Specified!\r\n{ex}", false, ConsoleColor.Red); error = true;
                };
                if (ApplicationConfig.Servers == null || ApplicationConfig.Servers.Length == 0)
                {
                    WriteConsole("No Servers Specified!", false, ConsoleColor.Red); error = true;
                };
                if (ApplicationConfig.Tasks == null || ApplicationConfig.Tasks.Length == 0)
                {
                    WriteConsole("No Tasks Specified!", false, ConsoleColor.Red); error = true;
                };
                if (error) { LoopPROC.Exit(1); };
            };

            // LAUNCH FROM COMMAND LINE ARGUMENTS
            LaunchFromCmdLine(args);

            // START SENDER
            Thread.Sleep(250);
            WriteConsole("Starting loop, press Enter to Exit...", true, ConsoleColor.Cyan);
            using (LoopRUNNER service = new LoopRUNNER(args))
            {
                service.Start(args);
                Console.ReadLine(); // Enter to Exit

                // STOP SENDER
                WriteConsole("Stopping loop...", true, ConsoleColor.Cyan);
                service.Stop();
            };
            // EXIT SENDER
            WriteConsole("Stopped", true, ConsoleColor.Cyan);
            LoopPROC.Exit(0);
            return 0;
        }

        #endregion ENTRY POINT

        #region PRIVATE METHODS

        private static void LaunchFromCmdLine(string[] args)
        {
            foreach (string arg in args)
            {
                if (!arg.StartsWith("/")) continue;
                if (arg == "/help"      || arg == "-help")      PrintHelp();
                if (arg == "/service"   || arg == "-service")   RunAsService(args);
                if (arg == "/install"   || arg == "-install")   SvcTools.InstallSvc();                
                if (arg == "/start"     || arg == "-start")     SvcTools.StartSvc();
                if (arg == "/stop"      || arg == "-stop")      SvcTools.StopSvc();
                if (arg == "/uninstall" || arg == "-uninstall") SvcTools.UnInstallSvc();                
            };
        }

        private static void PrintHelp()
        {
            string text = "Command Line Arguments:\r\n\r\n" +
                $"- [file.xml]   -- (XML Config File)\r\n" +
                $"- /help        -- ({ApplicationTitle.Trim()} Help)\r\n" +
                $"- /service     -- (run as win service)\r\n" +
                $"- /install     -- (install win service)\r\n" +
                $"- /uninstall   -- (uninstall win service)\r\n" +
                $"- /start       -- (start win service)\r\n" +
                $"- /stop        -- (stop win service)";
            WriteConsole(text, false, ConsoleColor.Yellow);
            LoopPROC.Exit(2);
        }

        private static void RunAsService(string[] args)
        {
            if (Environment.UserInteractive)
                LoopPROC.WriteConsole($"Please use `Service Manager` to run {LoopPROC.ApplicationCaption} as Service", false, ConsoleColor.Red);
            try { ServiceBase.Run(new ServiceBase[] { new LoopRUNNER(args) });}
            catch { LoopPROC.Exit(11); };
            LoopPROC.Exit(3);
        }

        #endregion PRIVATE METHODS

        #region LOOP RUNNER

        private class LoopRUNNER : ServiceBase
        {
            #region PRIVATES
            private APRSClient client           = null;
            private Thread     main             = null;
            private int        serverIndex      =   -1;
            private static string[] cmdLineArgs = null;
            private static int loopInterval     = 2500;
            private Dictionary<int, (DateTime, int)> taskPasses = new Dictionary<int, (DateTime, int)>();
            #endregion PRIVATES

            #region Public Params

            public ulong incmPcktCtr { private set; get; } = 0;
            public ulong otgnPcktCtr { private set; get; } = 0;            
            public static bool running { private set; get; } = false;

            #endregion Public Params

            #region Constructor

            public LoopRUNNER() { }
            public LoopRUNNER(string[] args) => cmdLineArgs = args == null ? cmdLineArgs : args;

            #endregion Constructor

            #region Start/Stop

            protected override void OnStart(string[] args) => 
                Start(args);
            internal void Start(string[] args) { running = true; (main = new Thread(LoopThread)).Start(); }
            protected override void OnStop() => 
                Stop();
            internal void Stop() { running = false; main.Join(); }

            #endregion Start/Stop

            #region Check Valid APRS Message

            private bool IsValidAPRSMessage(string message)
            {
                APRSParser parser = new APRSParser();
                parser.Parse(message);
                return !string.IsNullOrEmpty(parser.Callsign);
            }

            #endregion Check Valid APRS Message

            #region LOOPER

            private void LoopThread()
            {
                while (running)
                {
                    try { LoopStep(); }
                    catch (Exception ex) { WriteConsole($"{ex}", true, ConsoleColor.Red); };
                    Thread.Sleep(loopInterval);
                };
                Unloop();
            }

            public void LoopStep()
            {
                DateTime lastStatusLoop = DateTime.UtcNow;
                // LOOP THROUOUT SERVER, CONNECT, RECONNECT
                if (client == null)
                {
                    for (int srv_id = 0; srv_id < ApplicationConfig.Servers.Length; srv_id++)
                    {
                        try
                        {
                            serverIndex = srv_id;

                            client = new APRSClient(ApplicationConfig.Servers[srv_id].sever, ApplicationConfig.Servers[srv_id].port, ApplicationConfig.Servers[srv_id].user, ApplicationConfig.Servers[srv_id].pass, ApplicationConfig.Servers[srv_id].filter, ApplicationConfig.Servers[srv_id].ping);
                            if (ApplicationConfig.Servers[srv_id].readIncomingPackets)
                            {
                                client.OnPacket = (string l, APRSParser d) =>
                                {
                                    incmPcktCtr++;
                                    WriteConsole(new string[] { $"Incoming packet from {ApplicationConfig.Servers[serverIndex].sever}:", l }, true, ConsoleColor.Gray);
                                };
                                client.OnLocation = (string l, APRSParser d) => WriteConsole(new string[] { $"Incoming location from {ApplicationConfig.Servers[serverIndex].sever}:", l }, true, ConsoleColor.Gray);
                                client.OnWeather = (string l, APRSParser d) => WriteConsole(new string[] { $"Incoming weather from {ApplicationConfig.Servers[serverIndex].sever}:", l }, true, ConsoleColor.Gray);
                            }
                            else client.OnPacket = (string l, APRSParser d) => incmPcktCtr++;

                            client.Start();
                            Thread.Sleep(5000);

                            if (!client.Running || !client.Connected)
                            {
                                client.Stop();
                                client = null;
                                continue;
                            };

                            WriteConsole($"Connected to APRS Server {ApplicationConfig.Servers[srv_id].user}@{ApplicationConfig.Servers[srv_id].sever}:{ApplicationConfig.Servers[srv_id].port} with filter `{ApplicationConfig.Servers[srv_id].filter}`", true, ConsoleColor.Magenta);
                            WriteConsole($"APRS-IS Status: http://{ApplicationConfig.Servers[srv_id].sever}:14501/", true, ConsoleColor.Blue);
                            break;
                        }
                        catch (Exception ex) { WriteConsole($"{ex}", true, ConsoleColor.Red); };
                        client = null;
                    };
                };

                // CHECK CONNECTED
                if (client != null && !client.Connected) client = null;

                // LOOP THROUOUT TASKS AND MESSAGES
                if (client != null && client.Connected)
                {
                    for (int task_id = 0; task_id < ApplicationConfig.Tasks.Length; task_id++)
                    {
                        if (ApplicationConfig.Tasks[task_id].fromDate > DateTime.Now) continue;
                        if (ApplicationConfig.Tasks[task_id].tillDate < DateTime.Now) continue;

                        if (DateTime.TryParse(ApplicationConfig.Tasks[task_id].fromTime, out DateTime tf) && DateTime.TryParse(ApplicationConfig.Tasks[task_id].tillTime, out DateTime tt))
                        {
                            if (ApplicationConfig.Tasks[task_id].fromDate > tf) continue;
                            if (ApplicationConfig.Tasks[task_id].tillDate < tt) continue;

                            int msg_line = 0;
                            if (taskPasses.ContainsKey(task_id))
                            {
                                if ((DateTime.UtcNow - taskPasses[task_id].Item1).TotalSeconds < ApplicationConfig.Tasks[task_id].intervalSeconds) continue;
                                msg_line = taskPasses[task_id].Item2 + 1;
                            };
                            if (msg_line >= ApplicationConfig.Tasks[task_id].Commands.Count) msg_line = 0;

                            string cmd = ApplicationConfig.Tasks[task_id].Commands[msg_line];
                            try
                            {
                                if (!IsValidAPRSMessage(cmd))
                                {
                                    WriteConsole($"Invalid cmd task {task_id + 1}/{ApplicationConfig.Tasks.Length} cmd {msg_line + 1}/{ApplicationConfig.Tasks[task_id].Commands.Count}: {cmd}", true, ConsoleColor.Red);
                                    if (taskPasses.ContainsKey(task_id))
                                        taskPasses[task_id] = (DateTime.MinValue, msg_line);
                                    else
                                        taskPasses.Add(task_id, (DateTime.MinValue, msg_line));
                                    break;
                                };
                                if (client.SendToServer(cmd))
                                {
                                    otgnPcktCtr++;
                                    WriteConsole(new string[] { $"Send to {ApplicationConfig.Servers[serverIndex].sever} task {task_id + 1}/{ApplicationConfig.Tasks.Length} cmd {msg_line + 1}/{ApplicationConfig.Tasks[task_id].Commands.Count} next in {ApplicationConfig.Tasks[task_id].intervalSeconds}s:", cmd }, true, ConsoleColor.Green, ConsoleColor.Yellow);
                                }
                                else
                                    continue;
                            }
                            catch (Exception ex)
                            {
                                WriteConsole($"{ex}", true, ConsoleColor.Red);
                                client.Stop();
                                client = null;
                                break;
                            };

                            if (taskPasses.ContainsKey(task_id))
                                taskPasses[task_id] = (DateTime.UtcNow, msg_line);
                            else
                                taskPasses.Add(task_id, (DateTime.UtcNow, msg_line));
                            break;
                        };
                    };
                };

                if (Environment.UserInteractive) Console.Title = $"{ApplicationTitle.Trim()} - i{incmPcktCtr}/o{otgnPcktCtr}";
                if ((DateTime.UtcNow - lastStatusLoop).TotalMinutes >= 5)
                {
                    lastStatusLoop = DateTime.UtcNow;
                    WriteConsole($"{incmPcktCtr} packets received from {ApplicationConfig.Servers[serverIndex].sever}", true, ConsoleColor.Gray);
                    WriteConsole($"{otgnPcktCtr} packets send to {ApplicationConfig.Servers[serverIndex].sever}", true, ConsoleColor.Gray);
                };
            }
                        
            public void Unloop()
            {
                if (client != null)
                {
                    try { client.Stop(); } catch { };
                    client = null;
                    try
                    {
                        if (serverIndex >= 0)
                            WriteConsole($"Disconnected from APRS Server {ApplicationConfig.Servers[serverIndex].sever}", true, ConsoleColor.Magenta);
                    }
                    catch { };
                }
            }

            #endregion LOOPER
        }

        #endregion LOOP RUNNER

        #region CATCH EXCEPTIONS
        private static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
        {
            HandleUnhandledException(e.Exception);
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject != null)
            {
                Exception ex = (Exception)e.ExceptionObject;
                HandleUnhandledException(ex);
            }
        }

        private static void HandleUnhandledException(Exception ex)
        {
            WriteConsole(String.Format("Exception: {0}: {1} Source: {2} {3}", ex.GetType(), ex.Message, ex.Source, ex.StackTrace), true, ConsoleColor.Red);
        }

        #endregion CATCH EXCEPTIONS
    }
}
