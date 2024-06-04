//
// C#
// dkxce APRS Client Loop Sender
// v 0.3, 04.06.2024
// https://github.com/dkxce/APRSClientLoopSender
// en,ru,1251,utf-8
//

using System.IO;
using System.Xml.Serialization;

namespace System.Xml
{
    [Serializable]
    public class XMLSaved<T>
    {
        /// <summary>
        ///     Сохранение структуры в файл
        /// </summary>
        /// <param name="file">Полный путь к файлу</param>
        /// <param name="obj">Структура</param>
        public static void Save(string file, T obj)
        {
            XmlSerializerNamespaces ns = new XmlSerializerNamespaces(); ns.Add("", "");
            XmlSerializer xs = new XmlSerializer(typeof(T));
            StreamWriter writer = File.CreateText(file);
            xs.Serialize(writer, obj, ns);
            writer.Flush();
            writer.Close();
        }

        public static void SaveHere(string file, T obj)
        {
            Save(Path.Combine(CurrentDirectory(), file), obj);
        }

        public static string Save(T obj)
        {
            XmlSerializerNamespaces ns = new XmlSerializerNamespaces(); ns.Add("", "");
            XmlSerializer xs = new XmlSerializer(typeof(T));
            MemoryStream ms = new MemoryStream();
            StreamWriter writer = new StreamWriter(ms);
            xs.Serialize(writer, obj, ns);
            writer.Flush();
            ms.Position = 0;
            byte[] bb = new byte[ms.Length];
            ms.Read(bb, 0, bb.Length);
            writer.Close();
            return Text.Encoding.UTF8.GetString(bb); ;
        }

        /// <summary>
        ///     Подключение структуры из файла
        /// </summary>
        /// <param name="file">Полный путь к файлу</param>
        /// <returns>Структура</returns>
        public static T Load(string file)
        {
            try
            {
                XmlSerializer xs = new XmlSerializer(typeof(T));
                StreamReader reader = File.OpenText(file);
                T c = (T)xs.Deserialize(reader);
                reader.Close();
                return c;
            }
            catch { };
            {
                Type type = typeof(T);
                Reflection.ConstructorInfo c = type.GetConstructor(new Type[0]);
                return (T)c.Invoke(null);
            };
        }

        public static T LoadHere(string file)
        {
            return Load(Path.Combine(CurrentDirectory(), file));
        }

        public static T Load()
        {
            try { return Load(CurrentDirectory() + @"\config.xml"); }
            catch { };
            Type type = typeof(T);
            Reflection.ConstructorInfo c = type.GetConstructor(new Type[0]);
            return (T)c.Invoke(null);
        }

        public static string CurrentDirectory()
        {
            return AppDomain.CurrentDomain.BaseDirectory;
            // return Application.StartupPath;
            // return Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            // return Directory.GetCurrentDirectory();
            // return Environment.CurrentDirectory;
            // return Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase);
            // return Path.GetDirectory(Application.ExecutablePath);
        }
    }
}
