//
// C#
// dkxce APRS Client Loop Sender
// v 0.1, 03.06.2024
// https://github.com/dkxce/APRSClientLoopSender
// en,ru,1251,utf-8
//

using System;
using System.Collections.Generic;
using System.Threading;
using System.Xml;

namespace APRSClientLoopSender
{
    internal class Program
    {
        private static string    title   = "dkxce APRS Client Loop Sender v0.1 ";
        private static XMLConfig config  = null;
        private static bool      running = true;
        private static Thread    main    = null;
        private static Looper    looper  = null;

        #region WriteConsole

        private static void WriteConsole(string msg, bool withDT = false, ConsoleColor? color = null, ConsoleColor? firstLineColor = null)
        {
            WriteConsole(new string[] { msg }, withDT, color, firstLineColor);
        }

        private static void WriteConsole(string[] msg, bool withDT = false, ConsoleColor? color = null, ConsoleColor? firstLineColor = null)
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
        }


        #endregion WriteConsole

        private class Looper
        {
            private APRSClient client     = null;
            private int        serverIndex = -1;
            private Dictionary<int, (DateTime,int)> taskPasses = new Dictionary<int, (DateTime, int)>();

            private bool IsValidCmd(string cmd)
            {
                APRSParser parser = new APRSParser();
                parser.Parse(cmd);
                return !string.IsNullOrEmpty(parser.Callsign);
            }

            public void Loop()
            {
                // LOOP THROUOUT SERVER, CONNECT, RECONNECT
                if (client == null) 
                {
                    for(int srv_id=0;srv_id<config.Servers.Length;srv_id++)
                    {
                        try
                        {
                            serverIndex = srv_id;
                            
                            client = new APRSClient(config.Servers[srv_id].sever, config.Servers[srv_id].port, config.Servers[srv_id].user, config.Servers[srv_id].pass, config.Servers[srv_id].filter, config.Servers[srv_id].ping);
                            client.OnPacket   = (string l, APRSParser d) => WriteConsole(new string[] { $"Incoming packet from {config.Servers[serverIndex].sever}:",   l }, true, ConsoleColor.Gray);
                            client.OnLocation = (string l, APRSParser d) => WriteConsole(new string[] { $"Incoming location from {config.Servers[serverIndex].sever}:", l }, true, ConsoleColor.Gray);
                            client.OnWeather  = (string l, APRSParser d) => WriteConsole(new string[] { $"Incoming weather from {config.Servers[serverIndex].sever}:",  l }, true, ConsoleColor.Gray);                            
                            
                            client.Start();
                            Thread.Sleep(5000);

                            if(!client.Running || !client.Connected)
                            {
                                client.Stop();
                                client = null;
                                continue;
                            };  
                            
                            WriteConsole($"Connected to APRS Server {config.Servers[srv_id].user}@{config.Servers[srv_id].sever}:{config.Servers[srv_id].port} with filter `{config.Servers[srv_id].filter}`", true, ConsoleColor.Magenta);                            
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
                    for(int task_id=0;task_id<config.Tasks.Length;task_id++)
                    {
                        if (config.Tasks[task_id].fromDate > DateTime.Now) continue;
                        if (config.Tasks[task_id].tillDate < DateTime.Now) continue;

                        if(DateTime.TryParse(config.Tasks[task_id].fromTime, out DateTime tf) && DateTime.TryParse(config.Tasks[task_id].tillTime, out DateTime tt))
                        {
                            if (config.Tasks[task_id].fromDate > tf) continue;
                            if (config.Tasks[task_id].tillDate < tt) continue;

                            int msg_line = 0;
                            if(taskPasses.ContainsKey(task_id))
                            {
                                if ((DateTime.UtcNow - taskPasses[task_id].Item1).TotalSeconds < config.Tasks[task_id].intervalSeconds) continue;
                                msg_line = taskPasses[task_id].Item2 + 1; 
                            };
                            if (msg_line >= config.Tasks[task_id].Commands.Count) msg_line = 0;

                            string cmd = config.Tasks[task_id].Commands[msg_line];
                            try
                            {
                                if(!IsValidCmd(cmd))
                                {
                                    WriteConsole($"Invalid cmd task {task_id + 1}/{config.Tasks.Length} cmd {msg_line + 1}/{config.Tasks[task_id].Commands.Count}: {cmd}", true, ConsoleColor.Red);
                                    if (taskPasses.ContainsKey(task_id))
                                        taskPasses[task_id] = (DateTime.MinValue, msg_line);
                                    else
                                        taskPasses.Add(task_id, (DateTime.MinValue, msg_line));
                                    break;
                                };
                                if (client.SendToServer(cmd))
                                    WriteConsole(new string[] { $"Send to {config.Servers[serverIndex].sever} task {task_id + 1}/{config.Tasks.Length} cmd {msg_line + 1}/{config.Tasks[task_id].Commands.Count} next in {config.Tasks[task_id].intervalSeconds}s:", cmd }, true, ConsoleColor.Green, ConsoleColor.Yellow);
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
            }

            public void Unloop()
            {
                if (client != null) 
                {
                    client.Stop();
                    client = null;
                    if (serverIndex >= 0)
                        WriteConsole($"Disconnected from APRS Server {config.Servers[serverIndex].sever}", true, ConsoleColor.Magenta);
                }
            }
        }

        private static void MainThread()
        {
            looper = new Looper();
            while(running)
            {
                try { looper.Loop(); }
                catch (Exception ex) { WriteConsole($"{ex}", true, ConsoleColor.Red); };
                Thread.Sleep(2500);
            };
            looper.Unloop();
        }

        static int Main(string[] args)
        {
            Console.Title = title;

            string file = "APRSClientLoopSender.xml";
            if (!System.IO.File.Exists(System.IO.Path.Combine(XMLSaved<int>.CurrentDirectory(), file))) XMLConfig.SaveTemplate();
            foreach (string a in args) { try { if (System.IO.File.Exists(a)) file = a; } catch { }; }            

            // HEADER
            Console.WriteLine("*********************************************\r\n*                                           *");
            Console.WriteLine($"*    {title}    *");
            Console.WriteLine("*                                           *\r\n*********************************************");
            Console.WriteLine("https://github.com/dkxce/APRSClientLoopSender");
            Console.WriteLine();

            // CONFIG
            WriteConsole(String.Format("Configuration: {0}",  System.IO.Path.GetFileName(file)), false, ConsoleColor.Yellow);
            Console.WriteLine();

            // CHECK CONFIG
            bool error = false;
            try { config = XMLConfig.LoadNormal(file); } catch (Exception ex)
            {
                WriteConsole($"No Config Specified!\r\n{ex}", false, ConsoleColor.Red);
                error = true;
            };
            if(config.Servers == null || config.Servers.Length == 0)
            {
                WriteConsole("No Servers Specified!", false, ConsoleColor.Red);
                error = true;
            };
            if (config.Tasks == null || config.Tasks.Length == 0)
            {
                WriteConsole("No Tasks Specified!", false, ConsoleColor.Red);
                error = true;
            };
            if(error)
            {
                Thread.Sleep(2500);
                return 1;
            };

            // START
            Thread.Sleep(250);
            WriteConsole("Starting loop, press Enter to Exit...", true, ConsoleColor.Cyan);
            (main = new Thread(MainThread)).Start();
            Console.ReadLine();

            // STOP
            running = false;
            WriteConsole("Stopping loop...", true, ConsoleColor.Cyan);
            main.Join();

            // EXIT
            WriteConsole("Stopped", true, ConsoleColor.Cyan);
            Thread.Sleep(2500);
            return 0;
        }
    }
}
