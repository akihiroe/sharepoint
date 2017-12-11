using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace SharePointExplorer.Models
{
    public class ExplorerSettings
    {

        public static ExplorerSettings Instance { get; set; }

        static ExplorerSettings()
        {
            try
            {
                Instance = Load();
            }
            catch (Exception)
            {

            }
            Instance = Instance ?? new ExplorerSettings();
        }

        public DateTime? StartDate { get; set; }

        public string LicenseKey { get; set; }

        public List<ConnectionInfo> Connections { get; set; }

        public int FileNameWidth { set; get; }
        public int ModifiedDateWidth { get; set; }
        public int SizeWidth { get; set; }
        public int OwnerWidth { get; set; }
        public int CheckedOutWidth { get; set; }
        public int AccessRightWidth { get; set; }
        public string DateFormat { get; set; }

        public ExplorerSettings()
        {
            Connections = new List<ConnectionInfo>();
            FileNameWidth = 300;
            ModifiedDateWidth = 150;
            SizeWidth = 100;
            OwnerWidth = 150;
            CheckedOutWidth = 150;
            AccessRightWidth = 150;
            DateFormat = "";
        }

        private static ExplorerSettings Load()
        {
            var seri = new XmlSerializer(typeof(ExplorerSettings));
            var file = Path.Combine(Utils.ApplicationFolder, "ExplorerSettings.xml");
            if (!File.Exists(file)) return null;
            using (var st = new StreamReader(file))
            {
                return (ExplorerSettings)seri.Deserialize(st);
            }
        }

        public void Save()
        {
            var seri = new XmlSerializer(typeof(ExplorerSettings));
            if (!Directory.Exists(Utils.ApplicationFolder)) Directory.CreateDirectory(Utils.ApplicationFolder);
            var file = Path.Combine(Utils.ApplicationFolder, "ExplorerSettings.xml");
            using (var st = new StreamWriter(file))
            {
                seri.Serialize(st, this);
            }
        }
    }
}
