using System.IO;
using System.Reflection;
using System.Xml.Serialization;

namespace CopyExtension
{
    public class Settings
    {
        public bool ReserveWriting { get; set; }
        public bool ReserveReading { get; set; }
        public bool EnableCopy { get; set; }
        public bool EnableMove { get; set; }
        public bool EnableCompare { get; set; }
        public bool EnableHardlink { get; set; }
        public bool EnableNukeFolder { get; set; }
        public bool EnableNukeFile { get; set; }
        public bool DeleteOnCancel { get; set; }

        public static Settings Load()
        {
            var path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "settings.xml");
            var ser = new XmlSerializer(typeof(Settings));
            if (!File.Exists(path))
            {
                using (var s = File.OpenWrite(path))
                {
                    ser.Serialize(s, new Settings()
                    {
                        ReserveReading = false,
                        ReserveWriting = true,
                        EnableCompare = true,
                        EnableCopy = true,
                        EnableMove = true,
                        EnableHardlink = true,
                        EnableNukeFile = true,
                        EnableNukeFolder = true,
                        DeleteOnCancel = true
                    });
                }
            }
            using (var s = File.OpenRead(path))
            {
                return (Settings)ser.Deserialize(s);
            }
        }
    }
}
