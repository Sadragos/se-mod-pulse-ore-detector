using Sandbox.Definitions;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRageMath;
using System.Xml.Serialization;
using static Sandbox.Definitions.MyCubeBlockDefinition;
using ProtoBuf;
using VRage.Utils;
using PulseOreDetector.Proto;

namespace PulseOreDetector
{
    public class Config
    {

        public static MyConfig Instance;


        public static void Load()
        {
            if (MyAPIGateway.Utilities.FileExistsInWorldStorage("Config.xml", typeof(MyConfig)))
            {
                try
                {
                    TextReader reader = MyAPIGateway.Utilities.ReadFileInWorldStorage("Config.xml", typeof(MyConfig));
                    var xmlData = reader.ReadToEnd();
                    Instance = MyAPIGateway.Utilities.SerializeFromXML<MyConfig>(xmlData);
                    reader.Dispose();
                    MyLog.Default.WriteLine("PulseOreDetector Config found and loaded");
                }
                catch (Exception e)
                {
                    MyLog.Default.WriteLine("PulseOreDetector Config loading failed");
                }
            }

            ValidateData();
            Save();
        }

        private static void ValidateData()
        {
            if (Instance == null)
            {
                Instance = new MyConfig();
                Instance.OreNames.Add(new MyAlias("Iron", "Fe"));
                Instance.OreNames.Add(new MyAlias("Nickel", "Ni"));
                Instance.OreNames.Add(new MyAlias("Cobalt", "Co"));
                Instance.OreNames.Add(new MyAlias("Magnesium", "Mg"));
                Instance.OreNames.Add(new MyAlias("Silicon", "Si"));
                Instance.OreNames.Add(new MyAlias("Silver", "Ag"));
                Instance.OreNames.Add(new MyAlias("Gold", "Au"));
                Instance.OreNames.Add(new MyAlias("Platinum", "Pt"));
                Instance.OreNames.Add(new MyAlias("Uranium", "U"));

                Instance.SizeDescriptions.Add(new MySize(2, "Tiny"));
                Instance.SizeDescriptions.Add(new MySize(10, "Small"));
                Instance.SizeDescriptions.Add(new MySize(25, "Medium"));
                Instance.SizeDescriptions.Add(new MySize(45, "Large"));
                Instance.SizeDescriptions.Add(new MySize(70, "Huge"));
                Instance.SizeDescriptions.Add(new MySize(100, "Massive"));
            }
        }

        public static void Save()
        {
            try
            {
                MyLog.Default.WriteLine("PulseOreDetector Serializing Config to XML... ");
                string xml = MyAPIGateway.Utilities.SerializeToXML(Instance);
                MyLog.Default.WriteLine("PulseOreDetector Writing Config to disk... ");
                TextWriter writer = MyAPIGateway.Utilities.WriteFileInWorldStorage("Config.xml", typeof(MyConfig));
                writer.Write(xml);
                writer.Flush();
                writer.Close();
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine("PulseOreDetector Error saving Config XML!" + e.StackTrace);
            }
        }
    }
}
