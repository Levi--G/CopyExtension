using System.IO;
using System.Reflection;
using System.Xml.Serialization;

namespace CopyExtension
{
    public class Settings
    {
        public bool ReserveWriting { get; set; } = true;
        public bool ReserveReading { get; set; } = false;
        public bool EnableCopy { get; set; } = true;
        public bool EnableMove { get; set; } = true;
        public bool EnableCompare { get; set; } = true;
        public bool EnableHardlink { get; set; } = true;
        public bool EnableNukeFolder { get; set; } = true;
        public bool EnableNukeFile { get; set; } = true;
        public int SmallFileLimit { get; set; } = 10;
        public long SmallFileSize { get; set; } = 5 * 1024 * 1024;


        public static Settings Load()
        {
            var path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "settings.xml");
            var ser = new XmlSerializer(typeof(Settings));
            if (!File.Exists(path))
            {
                using (var s = File.OpenWrite(path))
                {
                    ser.Serialize(s, new Settings());
                }
            }
            using (var s = File.OpenRead(path))
            {
                return (Settings)ser.Deserialize(s);
            }
        }
    }
}
