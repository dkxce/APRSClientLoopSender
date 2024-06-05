// C#
// dkxce APRS Client Loop Sender
// v 0.4, 05.06.2024
// https://github.com/dkxce/APRSClientLoopSender
// en,ru,1251,utf-8
//

using System;
using System.IO;
using System.Collections.Generic;
using System.Threading;
using System.Xml;
using System.ServiceProcess;

namespace APRSClientLoopSender
{
    internal class LoopRUNNER : ServiceBase
    {
        private class TrackLoopInfo
        {
            internal class TrackPoint
            {
                private string _smb      = "/k";
                private string _callsign = "UNKNOWN";
                private string _comment  = "";
                private float _speed     = 60;

                public double Lat        = 55.55;
                public double Lon        = 37.55;
                
                public float Speed
                {
                    get { return _speed; }
                    set { _speed = value; if (_speed < 0) _speed = 0; if (_speed > 1850) _speed = 1850; }
                }
                public string Comment
                {
                    get { return _comment; }
                    set { _comment = value; if (string.IsNullOrEmpty(_comment)) _comment = ""; if (_comment.Length > 35) _comment = _comment.Substring(0, 35); }
                }
                public string Callsign 
                {
                    get { return _callsign; }
                    set { _callsign = value; if (string.IsNullOrEmpty(_callsign) || _callsign.Length > 10) _callsign = "UNKNOWN"; }
                }
                public string Symbol
                {
                    get { return _smb; }
                    set { _smb = value; if (string.IsNullOrEmpty(_smb) || _smb.Length != 2) _smb = "/k"; }
                }
                public double Latitude { set { Lat = value; } get { return Lat; } }
                public double Longitude { set { Lon = value; } get { return Lon; } }
                public double X { set { Lon = value; } get { return Lon; } }
                public double Y { set { Lat = value; } get { return Lat; } }
            }

            public int index = int.MinValue;
            public XMLConfig.APRSTrack track = null;
            public int point = 0;
            public DateTime next = DateTime.MinValue;
            public string file = null;
            public List<TrackPoint> points = new List<TrackPoint>();
            public string symbol = "/k";

            public TrackLoopInfo()
            { 
                try
                {
                    string fnm = Path.Combine(XMLSaved<int>.CurrentDirectory(), "APRSClientLoopSender.trk");
                    if (!File.Exists(fnm)) return;
                    string[] txts = File.ReadAllText(fnm).Split(new char[] { 't', 'p', 'n' }, StringSplitOptions.RemoveEmptyEntries);
                    index = int.Parse(txts[0].Trim()) * -1 - 1;
                    point = int.Parse(txts[1].Trim());
                    if (txts.Length > 2 && DateTime.TryParse(txts[2].Trim(), out next)) next = next.ToUniversalTime();
                }
                catch { };
            }

            public void Save()
            {
                try 
                {
                    string txt = $"t {index} p {point} n {next.ToLocalTime()}";
                    string fnm = Path.Combine(XMLSaved<int>.CurrentDirectory(), "APRSClientLoopSender.trk");
                    File.WriteAllText(fnm, txt); 
                } catch { };
            }

            public void Clear() 
            { 
                index = -1; 
                track = null; 
                point = 0; 
                file = null; 
                points.Clear(); 
            }
        }

        #region PRIVATES
        private static int      minUpdateIntrvl =   30; // in sec
        private static string[] cmdLineArgs     = null; // environ
        private static int      loopInterval    = 2500; // in ms
        private static int      infoInterval    =    5; // in min
        private static int      saveInterval    =   30; // in sec

        private APRSClient      client          = null;
        private Thread          main            = null;
        private int             serverIndex     =   -1;
        private TrackLoopInfo   trackLoopInfo   = new TrackLoopInfo();
        private Dictionary<int, (DateTime, int)> taskPasses = new Dictionary<int, (DateTime, int)>();        
        #endregion PRIVATES

        #region Public Params

        public ulong icomingPacketCounter  { private set; get; } = 0;
        public ulong outgoingPacketCounter { private set; get; } = 0;
        public static bool running { private set; get; } = false;

        #endregion Public Params

        #region Constructor

        public LoopRUNNER(string[] args) => cmdLineArgs = args == null ? cmdLineArgs : args;

        #endregion Constructor

        #region Start/Stop

        protected override void OnStart(string[] args) => Start(args);
        internal void Start(string[] args) { running = true; (main = new Thread(LoopThread)).Start(); }
        protected override void OnStop() => Stop();
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
            ulong counter = 0;
            while (running)
            {
                try { LoopStep(counter++); }
                catch (Exception ex) { LoopPROC.WriteConsole($"{ex}", true, ConsoleColor.Red); };
                Thread.Sleep(loopInterval);
            };
            Unloop();
        }

        private void LoopStep(ulong counter)
        {
            LoopConnect(counter);
            LoopTasks(counter);
            LoopTracks(counter);
            LoopStatus(counter);
        }

        private void LoopConnect(ulong counter)
        {
            if (client == null)
            {
                for (int srv_id = 0; srv_id < LoopPROC.ApplicationConfig.Servers.Length; srv_id++)
                {
                    try
                    {
                        serverIndex = srv_id;

                        client = new APRSClient(LoopPROC.ApplicationConfig.Servers[srv_id].sever, LoopPROC.ApplicationConfig.Servers[srv_id].port, LoopPROC.ApplicationConfig.Servers[srv_id].user, LoopPROC.ApplicationConfig.Servers[srv_id].pass, LoopPROC.ApplicationConfig.Servers[srv_id].filter, LoopPROC.ApplicationConfig.Servers[srv_id].ping);
                        if (LoopPROC.ApplicationConfig.Servers[srv_id].readIncomingPackets)
                        {
                            client.OnPacket = (string l, APRSParser d) =>
                            {
                                icomingPacketCounter++;
                                LoopPROC.WriteConsole(new string[] { $"Incoming packet from {LoopPROC.ApplicationConfig.Servers[serverIndex].sever}:", l }, true, ConsoleColor.Gray);
                            };
                            client.OnLocation = (string l, APRSParser d) => LoopPROC.WriteConsole(new string[] { $"Incoming location from {LoopPROC.ApplicationConfig.Servers[serverIndex].sever}:", l }, true, ConsoleColor.Gray);
                            client.OnWeather = (string l, APRSParser d) => LoopPROC.WriteConsole(new string[] { $"Incoming weather from {LoopPROC.ApplicationConfig.Servers[serverIndex].sever}:", l }, true, ConsoleColor.Gray);
                        }
                        else
                        {
                            client.OnPacket = (string l, APRSParser d) => icomingPacketCounter++;
                        };

                        client.Start();
                        Thread.Sleep(5000);

                        if (!client.Running || !client.Connected)
                        {
                            client.Stop();
                            client = null;
                            continue;
                        };

                        LoopPROC.WriteConsole($"Connected to APRS Server {LoopPROC.ApplicationConfig.Servers[srv_id].user}@{LoopPROC.ApplicationConfig.Servers[srv_id].sever}:{LoopPROC.ApplicationConfig.Servers[srv_id].port} with filter `{LoopPROC.ApplicationConfig.Servers[srv_id].filter}`", true, ConsoleColor.Magenta);
                        LoopPROC.WriteConsole($"APRS-IS Status: http://{LoopPROC.ApplicationConfig.Servers[srv_id].sever}:14501/", true, ConsoleColor.Blue);
                        LoopPROC.WriteConsole($"Web Map on: https://aprs-map.info/", true, ConsoleColor.Blue);
                        break;
                    }
                    catch (Exception ex) { LoopPROC.WriteConsole($"{ex}", true, ConsoleColor.Red); };
                    client = null;
                };
            };
            if (client != null && !client.Connected) client = null;
        }

        private void LoopTasks(ulong counter)
        {
            if (client != null && client.Connected)
            {
                for (int task_id = 0; task_id < LoopPROC.ApplicationConfig.Tasks.Length; task_id++)
                {
                    if (LoopPROC.ApplicationConfig.Tasks[task_id].fromDate > DateTime.Now) continue;
                    if (LoopPROC.ApplicationConfig.Tasks[task_id].tillDate < DateTime.Now) continue;

                    if (DateTime.TryParse(LoopPROC.ApplicationConfig.Tasks[task_id].fromTime, out DateTime tf) && DateTime.TryParse(LoopPROC.ApplicationConfig.Tasks[task_id].tillTime, out DateTime tt))
                    {
                        if (DateTime.Now < tf) continue;
                        if (DateTime.Now > tt) continue;
                    };

                    // OK
                    {
                        int msg_line = 0;
                        if (taskPasses.ContainsKey(task_id))
                        {
                            if ((DateTime.UtcNow - taskPasses[task_id].Item1).TotalSeconds < LoopPROC.ApplicationConfig.Tasks[task_id].intervalSeconds) continue;
                            msg_line = taskPasses[task_id].Item2 + 1;
                        };
                        if (msg_line >= LoopPROC.ApplicationConfig.Tasks[task_id].Commands.Count) msg_line = 0;

                        string cmd = LoopPROC.ApplicationConfig.Tasks[task_id].Commands[msg_line];
                        try
                        {
                            if (!IsValidAPRSMessage(cmd))
                            {
                                LoopPROC.WriteConsole($"Invalid cmd task {task_id + 1}/{LoopPROC.ApplicationConfig.Tasks.Length} cmd {msg_line + 1}/{LoopPROC.ApplicationConfig.Tasks[task_id].Commands.Count}: {cmd}", true, ConsoleColor.Red);
                                if (taskPasses.ContainsKey(task_id))
                                    taskPasses[task_id] = (DateTime.MinValue, msg_line);
                                else
                                    taskPasses.Add(task_id, (DateTime.MinValue, msg_line));
                                break;
                            };
                            if (client.SendToServer(cmd))
                            {
                                outgoingPacketCounter++;
                                LoopPROC.WriteConsole(new string[] { $"Send to {LoopPROC.ApplicationConfig.Servers[serverIndex].sever} task {task_id + 1}/{LoopPROC.ApplicationConfig.Tasks.Length} cmd {msg_line + 1}/{LoopPROC.ApplicationConfig.Tasks[task_id].Commands.Count} next in {LoopPROC.ApplicationConfig.Tasks[task_id].intervalSeconds}s:", cmd }, true, ConsoleColor.Green, ConsoleColor.Yellow);
                            }
                            else continue;
                        }
                        catch (Exception ex)
                        {
                            LoopPROC.WriteConsole($"{ex}", true, ConsoleColor.Red);
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

        private void LoopTracks(ulong counter)
        {
            if (client != null && client.Connected)
            {
                if (trackLoopInfo.index < 0)
                    for (int i = 0; i < LoopPROC.ApplicationConfig.Tracks.Length; i++)
                    {
                        if (LoopPROC.ApplicationConfig.Tracks[i].fromDate > DateTime.UtcNow) continue;
                        if (LoopPROC.ApplicationConfig.Tracks[i].tillDate < DateTime.UtcNow) continue;
                        if (trackLoopInfo.index != int.MinValue && trackLoopInfo.index < 0 && LoopPROC.ApplicationConfig.Tracks[i].id != ((trackLoopInfo.index + 1) * -1)) continue;

                        string fn = LoopPROC.ApplicationConfig.Tracks[i].file.Contains(":") ? LoopPROC.ApplicationConfig.Tracks[i].file : Path.Combine(XMLSaved<int>.CurrentDirectory(), LoopPROC.ApplicationConfig.Tracks[i].file);
                        if (File.Exists(fn))
                        {
                            // trackLoopInfo.next = DateTime.UtcNow;
                            trackLoopInfo.track = LoopPROC.ApplicationConfig.Tracks[trackLoopInfo.index = i];
                            trackLoopInfo.file = fn;
                            string pTxt = trackLoopInfo.point == 0 ? "start" : $"point {trackLoopInfo.point + 1}";
                            LoopPROC.WriteConsole($"Select track {trackLoopInfo.track.id} for tracking from {pTxt}, run at {trackLoopInfo.next.ToLocalTime()}", true, ConsoleColor.DarkYellow);
                            break;
                        };
                    };
                if (trackLoopInfo.index >= 0)
                {
                    LoopTrack(counter);
                    ulong saveDel = (ulong)(saveInterval * 1000 / loopInterval);
                    if (counter % saveDel == 0) trackLoopInfo.Save();
                };
            };
        }

        private void LoopTrack(ulong counter)
        {
            // FILL TRACK POINTS
            if (trackLoopInfo.points.Count == 0)
            {
                using (StreamReader reader = new StreamReader(trackLoopInfo.file))
                {
                    List<string> HDR = new List<string>(reader.ReadLine().ToLower().Split(';'));
                    for (int i = 0; i < HDR.Count; i++) HDR[i] = HDR[i].Substring(0, 2);
                    while (!reader.EndOfStream)
                    {
                        string[] values = reader.ReadLine().Split(';');
                        TrackLoopInfo.TrackPoint p = new TrackLoopInfo.TrackPoint() { Symbol = trackLoopInfo.symbol };
                        p.Lat = double.Parse(values[HDR.IndexOf("la")], System.Globalization.CultureInfo.InvariantCulture);
                        p.Lon = double.Parse(values[HDR.IndexOf("lo")], System.Globalization.CultureInfo.InvariantCulture);
                        if (HDR.IndexOf("sy") >= 0) p.Symbol = values[HDR.IndexOf("sy")];
                        if (HDR.IndexOf("sp") >= 0) p.Speed = float.Parse(values[HDR.IndexOf("sp")], System.Globalization.CultureInfo.InvariantCulture);
                        if (HDR.IndexOf("ca") >= 0) p.Callsign = values[HDR.IndexOf("ca")];
                        if (HDR.IndexOf("co") >= 0) p.Comment = values[HDR.IndexOf("co")];
                        trackLoopInfo.points.Add(p);
                    };
                };
            };

            // CHECK DATE RANGE
            if (DateTime.TryParse(trackLoopInfo.track.fromTime, out DateTime tf) && DateTime.TryParse(trackLoopInfo.track.tillTime, out DateTime tt))
            {
                if (DateTime.Now < tf) return;
                if (DateTime.Now > tt) return;
            };

            // FOLLOW TRACK POINTS
            if (trackLoopInfo.points.Count > 0)
            {
                if (trackLoopInfo.next > DateTime.UtcNow) return;

                TrackLoopInfo.TrackPoint p = trackLoopInfo.points[trackLoopInfo.point];
                Random rnd = new Random();
                float currSpeed = ((p.Speed + (float)trackLoopInfo.track.speedDeviation - (float)rnd.Next(0, 2 * trackLoopInfo.track.speedDeviation)) / 3.6f); // speed_deviation //
                if (currSpeed < trackLoopInfo.track.speedDeviation / 2) currSpeed = trackLoopInfo.track.speedDeviation / 2;
                if (currSpeed <= 0) currSpeed = 1;

                int angle = 0;
                if (trackLoopInfo.point > 0)
                    angle = (int)RadiansToDegrees(Bearing(trackLoopInfo.points[trackLoopInfo.point - 1], p));

                uint dist = 0;
                if (trackLoopInfo.point == trackLoopInfo.points.Count - 1)
                    ++trackLoopInfo.point;
                else
                {
                    uint need_dist = (uint)((float)minUpdateIntrvl * currSpeed);
                    while ((trackLoopInfo.point < trackLoopInfo.points.Count - 1) && (dist < need_dist))
                        dist = GetDistInMeters(trackLoopInfo.points[++trackLoopInfo.point - 1], trackLoopInfo.points[trackLoopInfo.point]);
                };

                int nextTime = minUpdateIntrvl;
                float move_speed = (float)((float)dist / (float)minUpdateIntrvl); // speed to next point
                if (move_speed > currSpeed) // speed is more than in csv
                {
                    nextTime = (int)((float)dist / currSpeed); // time to next point
                    move_speed = currSpeed; // speed to next point 
                };
                trackLoopInfo.next = DateTime.UtcNow.AddSeconds(nextTime);

                string pckt = PrepareAPRSTrackData(p, angle, move_speed);
                string cmd = $"{p.Callsign}>APRS:{pckt}";
                try
                {
                    if (IsValidAPRSMessage(cmd) && client.SendToServer(cmd))
                    {
                        outgoingPacketCounter++;
                        if (trackLoopInfo.point == trackLoopInfo.points.Count)
                            LoopPROC.WriteConsole(new string[] { $"Send Track to {LoopPROC.ApplicationConfig.Servers[serverIndex].sever} t{trackLoopInfo.track.id}p{trackLoopInfo.point}/{trackLoopInfo.points.Count}, no next:", cmd }, true, ConsoleColor.Green, ConsoleColor.Yellow);
                        else
                            LoopPROC.WriteConsole(new string[] { $"Send Track to {LoopPROC.ApplicationConfig.Servers[serverIndex].sever} t{trackLoopInfo.track.id}p{trackLoopInfo.point}/{trackLoopInfo.points.Count}, next {trackLoopInfo.next.ToLocalTime()}:", cmd }, true, ConsoleColor.Green, ConsoleColor.Yellow);
                    };
                }
                catch (Exception ex)
                {
                    LoopPROC.WriteConsole($"{ex}", true, ConsoleColor.Red);
                };
            };

            // SET NEXT TRACK
            if (trackLoopInfo.point == trackLoopInfo.points.Count)
            {
                int loopNext = trackLoopInfo.track.loopNext;
                int nextDelay = trackLoopInfo.track.nextDelay;
                trackLoopInfo.Clear();
                if (loopNext < 0) return;

                for (int i = 0; i < LoopPROC.ApplicationConfig.Tracks.Length; i++)
                {
                    if (LoopPROC.ApplicationConfig.Tracks[i].id != loopNext) continue;

                    if (LoopPROC.ApplicationConfig.Tracks[i].fromDate > DateTime.UtcNow) continue;
                    if (LoopPROC.ApplicationConfig.Tracks[i].tillDate < DateTime.UtcNow) continue;

                    string ffn = LoopPROC.ApplicationConfig.Tracks[i].file.Contains(":") ? LoopPROC.ApplicationConfig.Tracks[i].file : Path.Combine(XMLSaved<int>.CurrentDirectory(), LoopPROC.ApplicationConfig.Tracks[i].file);
                    if (!File.Exists(ffn)) return;

                    trackLoopInfo.next = DateTime.UtcNow.AddSeconds(nextDelay);
                    trackLoopInfo.track = LoopPROC.ApplicationConfig.Tracks[trackLoopInfo.index = i];
                    trackLoopInfo.file = ffn;
                    LoopPROC.WriteConsole($"Select track {trackLoopInfo.track.id} for tracking from start, run at {trackLoopInfo.next.ToLocalTime()}", true, ConsoleColor.DarkYellow);
                    return;
                };
            };
        }

        private void LoopStatus(ulong counter)
        {
            if (Environment.UserInteractive)
            {
                try {
                    Console.Title = $"{LoopPROC.ApplicationTitle.Trim()} - i{icomingPacketCounter}/o{outgoingPacketCounter}/t{trackLoopInfo.index}/p{trackLoopInfo.point}";
                } catch { };
            };
            ulong intrv = (ulong)(infoInterval * 60 * 1000 / loopInterval);
            if ((counter % intrv) == 0)
            {
                LoopPROC.WriteConsole($"{icomingPacketCounter} packets received from {LoopPROC.ApplicationConfig.Servers[serverIndex].sever}", true, ConsoleColor.Gray);
                LoopPROC.WriteConsole($"{outgoingPacketCounter} packets send to {LoopPROC.ApplicationConfig.Servers[serverIndex].sever}", true, ConsoleColor.Gray);
            };
        }

        private void Unloop()
        {
            if (client != null)
            {
                try { client.Stop(); } catch { };
                client = null;
                try
                {
                    if (serverIndex >= 0)
                        LoopPROC.WriteConsole($"Disconnected from APRS Server {LoopPROC.ApplicationConfig.Servers[serverIndex].sever}", true, ConsoleColor.Magenta);
                }
                catch { };
            }
        }

        #endregion LOOPER

        #region POINT UTILS

        private static double Bearing(TrackLoopInfo.TrackPoint a, TrackLoopInfo.TrackPoint b)
        {
            double x = Math.Cos(DegreesToRadians(a.Lat)) * Math.Sin(DegreesToRadians(b.Lat)) - Math.Sin(DegreesToRadians(a.Lat)) * Math.Cos(DegreesToRadians(b.Lat)) * Math.Cos(DegreesToRadians(b.Lon - a.Lon));
            double y = Math.Sin(DegreesToRadians(b.Lon - a.Lon)) * Math.Cos(DegreesToRadians(b.Lat));
            return (Math.Atan2(y, x) + Math.PI * 2) % (Math.PI * 2);
        }

        private static double DegreesToRadians(double a) => a * Math.PI / 180.0d;

        private static double RadiansToDegrees(double a) => a * 180.0d / Math.PI;

        private static uint GetDistInMeters(TrackLoopInfo.TrackPoint a, TrackLoopInfo.TrackPoint b) => (uint)GeoUtils.GetLengthMeters(a.Lat, a.Lon, b.Lat, b.Lon, false);

        private string PrepareAPRSTrackData(TrackLoopInfo.TrackPoint p, int angle, float speed)
        {
            // !5533.09N/03732.85Ey000/000/A=000131CMX865 Bell 202 AX.25 Testing                
            // =5533.09N/03732.85Ey000/000/A=000131CMX865 Bell 202 AX.25 Testing                
            string lat = LatLonParser.ToString(p.Lat, LatLonParser.FFormat.APRSLAT);
            string lon = LatLonParser.ToString(p.Lon, LatLonParser.FFormat.APRSLON);
            string crs = angle.ToString();
            while (crs.Length < 3) crs = "0" + crs;
            string spd = ((int)Math.Round(speed * 1.94384f)).ToString();
            while (spd.Length < 3) spd = "0" + spd;
            string pckt = $"!{lat}{p.Symbol[0]}{lon}{p.Symbol[1]}{crs}/{spd}{p.Comment}";
            return pckt;
        }


        #endregion POINT UTILS
    }

}
