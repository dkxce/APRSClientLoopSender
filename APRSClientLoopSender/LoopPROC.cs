//
// C#
// dkxce APRS Client Loop Sender
// v 0.4, 05.06.2024
// https://github.com/dkxce/APRSClientLoopSender
// en,ru,1251,utf-8
//

using System;
using System.IO;
using System.Threading;
using System.Xml;
using System.Windows.Forms;
using System.ServiceProcess;

namespace APRSClientLoopSender
{
    internal class LoopPROC
    {
        #region Application Main

        internal static string     ApplicationCaption { private set; get; } = "dkxce APRS Client Loop Sender";
        internal static string     ApplicationService { private set; get; } = ApplicationCaption.Replace("Client Loop Sender", "CLS").Replace(" ", ".");
        internal static string     ApplicationVersion { private set; get; } = "0.4";
        internal static string     ApplicationTitle   { private set; get; } = $"{ApplicationCaption} v{ApplicationVersion.PadRight(6)} ";
        internal static string     ApplicationWebSite { private set; get; } = "https://github.com/dkxce/APRSClientLoopSender";
        internal static XMLConfig  ApplicationConfig  { private set; get; } = null;
        internal static string     AppConfigFile      { private set; get; } = "APRSClientLoopSender.xml";
        internal static string     ApplicationLogFile
        {
            get 
            {
                string dt = DateTime.UtcNow.ToString("yyyyMMdd");
                string path = Path.Combine(XMLSaved<int>.CurrentDirectory(), @"LOGS\"+$"log_{dt}.txt");
                try { Directory.CreateDirectory(Path.GetDirectoryName(path)); } catch { };
                return path;
            }
        }

        #endregion Application Main

        #region INTERNAL METHODS

        internal static void WriteConsole(string msg, bool withDT = false, ConsoleColor? color = null, ConsoleColor? firstLineColor = null, bool? onlyLog = null)
        {
            WriteConsole(new string[] { msg }, withDT, color, firstLineColor, onlyLog);
        }

        internal static void WriteConsole(string[] msg, bool withDT = false, ConsoleColor? color = null, ConsoleColor? firstLineColor = null, bool? onlyLog = null)
        {
            if (!onlyLog.HasValue || onlyLog == false)
            {
                ConsoleColor cc = Console.ForegroundColor;
                if (color.HasValue || firstLineColor.HasValue) Console.ForegroundColor = ConsoleColor.White;
                if (withDT) Console.Write($"{DateTime.Now}: ");
                for (int msg_id = 0; msg_id < msg.Length; msg_id++)
                {
                    if (color.HasValue && ((msg_id == 0 && msg.Length == 1) || (msg_id == 1))) Console.ForegroundColor = color.Value;
                    if (firstLineColor.HasValue && msg_id == 0) Console.ForegroundColor = firstLineColor.Value;
                    Console.WriteLine(msg[msg_id]);
                };
                if (color.HasValue || firstLineColor.HasValue) Console.ForegroundColor = cc;
            };

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
                if (!File.Exists(Path.Combine(XMLSaved<int>.CurrentDirectory(), AppConfigFile))) XMLConfig.SaveTemplate();
                foreach (string a in args) if (!a.StartsWith("/")) { try { if (File.Exists(a)) AppConfigFile = a; } catch { }; };
            };

            // WRITE HEADER
            {
                Console.WriteLine("*********************************************\r\n*                                           *");
                Console.WriteLine($"*    {ApplicationTitle} *");
                Console.WriteLine($"*             {ApplicationService} {ApplicationVersion.PadRight(14)} * ");
                Console.WriteLine("*                                           *\r\n*********************************************");
                Console.WriteLine("https://github.com/dkxce/APRSClientLoopSender");
                Console.WriteLine("");
            };            

            // READ CONFIG
            {
                WriteConsole("******************************************************************************************", true, null, null, true);
                WriteConsole(String.Format("Launching: {0}", Environment.CommandLine), false, ConsoleColor.Yellow);
                WriteConsole(String.Format("Configuration: {0}", Path.GetFileName(AppConfigFile)), false, ConsoleColor.Yellow);
                WriteConsole("");
                bool error = false;
                try 
                { 
                    ApplicationConfig = XMLConfig.LoadNormal(AppConfigFile); 
                }
                catch (Exception ex)
                {
                    WriteConsole($"Bad configuration:\r\n{ex}", false, ConsoleColor.Red); error = true;
                };
                if (ApplicationConfig.Servers == null || ApplicationConfig.Servers.Length == 0)
                {
                    WriteConsole("No Servers Specified!", false, ConsoleColor.Red); error = true;
                };
                if (ApplicationConfig.Tasks == null || ApplicationConfig.Tasks.Length == 0)
                {
                    //WriteConsole("No Tasks Specified!", false, ConsoleColor.Red); error = true;
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
            Exit(0);
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
