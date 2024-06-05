//
// C#
// dkxce APRS Client Loop Sender
// v 0.4, 05.06.2024
// https://github.com/dkxce/APRSClientLoopSender
// en,ru,1251,utf-8
//

using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;

namespace APRSClientLoopSender
{
    public class XMLConfig: XMLSaved<XMLConfig>
    {
        public class APRSServer
        {
            [XmlAttribute]
            public bool on = false;
            [XmlAttribute]
            public int priority = 9999;
            [XmlAttribute]
            public string sever = "localhost";
            [XmlAttribute]
            public ushort port = 14580;
            [XmlAttribute]
            public string user = "UNKNOWN";
            [XmlAttribute]
            public string pass = "-1";
            [XmlAttribute]
            public string filter = "p/R/U";
            [XmlAttribute]
            public string ping = "APRS,TCPIP*:>online";
            [XmlAttribute]
            public bool readIncomingPackets = false;

            public class APRSSercerComparer: System.Collections.IComparer
            {
                public int Compare(object x, object y) => ((APRSServer)x).priority.CompareTo(((APRSServer)y).priority);
            }
        }

        public class APRSTask
        {
            [XmlAttribute]
            public bool on = false;
            [XmlAttribute]
            public int id = 0;
            [XmlAttribute]
            public string fromTime = "00:00:00";
            [XmlAttribute]
            public string tillTime = "23:59:59";
            [XmlAttribute]
            public DateTime fromDate = DateTime.Today;
            [XmlAttribute]
            public DateTime tillDate = DateTime.Today.AddYears(10);
            [XmlAttribute]
            public int intervalSeconds = 90;
            [XmlText]
            public string commands = "\r\nSFROM>STO,WIDE1-1,WIDE2-2:Simple Text\r\nSFROM>STO,WIDE1-1,WIDE2-2:>Simple Status\r\nSFROM>STO,WIDE1-1,WIDE2-2::TOALL    :Message. Hello All (I'm a teapot)\r\n";            

            [XmlIgnore]
            public List<string> Commands
            {
                get
                {
                    List<string> res = new List<string>();
                    if (!String.IsNullOrEmpty(commands)) res.AddRange(commands.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries));
                    for (int i = res.Count - 1; i >= 0; i--)
                    {
                        res[i] = res[i].Trim();
                        if (string.IsNullOrEmpty(res[i])) res.RemoveAt(i);
                    };
                    return res;
                }
            }
        }

        public class APRSTrack
        {
            [XmlAttribute]
            public bool on = false;
            [XmlAttribute]
            public int id = 0;
            [XmlAttribute]
            public int loopNext = -1; // Next ID
            [XmlAttribute]
            public int nextDelay = 4000; // in seconds
            [XmlAttribute]
            public int speedDeviation = 2; // KMPH
            [XmlAttribute]
            public string fromTime = "00:00:00";
            [XmlAttribute]
            public string tillTime = "23:59:59";
            [XmlAttribute]
            public DateTime fromDate = DateTime.Today;
            [XmlAttribute]
            public DateTime tillDate = DateTime.Today.AddYears(10);
            [XmlAttribute]
            public string file = ""; // Relative Path
        }

        [XmlArray("servers"), XmlArrayItem("server")]
        public APRSServer[] Servers = new APRSServer[0];
        [XmlArray("tasks"), XmlArrayItem("task")]
        public APRSTask[] Tasks = new APRSTask[0];
        [XmlArray("tracks"), XmlArrayItem("track")]
        public APRSTrack[] Tracks = new APRSTrack[0];

        public static XMLConfig LoadNormal(string file = null)
        {
            XMLConfig res = LoadHere(file == null ? "APRSClientLoopSender.xml" : file);
            if (res.Servers != null && res.Servers.Length > 0) Array.Sort(res.Servers, new APRSServer.APRSSercerComparer());
            List<APRSServer> srvs = new List<APRSServer>(res.Servers);
            for (int i = srvs.Count - 1; i >= 0; i--) if (!srvs[i].on) srvs.RemoveAt(i);
            res.Servers = srvs.ToArray();
            List<APRSTask> tsks = new List<APRSTask>(res.Tasks);
            for (int i = tsks.Count - 1; i >= 0; i--) if (!tsks[i].on) tsks.RemoveAt(i);
            res.Tasks = tsks.ToArray();
            List<APRSTrack> trks = new List<APRSTrack>(res.Tracks);
            for (int i = trks.Count - 1; i >= 0; i--) if (!trks[i].on && !string.IsNullOrEmpty(trks[i].file)) trks.RemoveAt(i);
            res.Tracks = trks.ToArray();
            return res;
        }

        public void Save()
        {
            SaveHere("APRSClientLoopSender.xml", this);
        }

        public static void SaveTemplate()
        {
            XMLConfig xc = new XMLConfig();
            xc.Servers = new APRSServer[] { new APRSServer() };
            xc.Tasks = new APRSTask[] { new APRSTask() };
            xc.Tracks = new APRSTrack[] { new APRSTrack() };
            xc.Save();
        }
    }
}
