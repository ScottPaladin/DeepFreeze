using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DF
{
    public class DFGameSettings
    {
        private const string configNodeName = "DFGameSettings";
        public bool Enabled { get; set; }
        public Dictionary<string, KerbalInfo> KnownFrozenKerbals { get; private set; }

        public DFGameSettings()
        {
            Enabled = true;
            KnownFrozenKerbals = new Dictionary<string, KerbalInfo>();
        }

        public void Load(ConfigNode node)
        {
            KnownFrozenKerbals.Clear();
            if (node.HasNode(configNodeName))
            {
                ConfigNode DFsettingsNode = node.GetNode(configNodeName);
                Enabled = Utilities.GetNodeValue(DFsettingsNode, "Enabled", Enabled);

                KnownFrozenKerbals.Clear();
                var kerbalNodes = DFsettingsNode.GetNodes(KerbalInfo.ConfigNodeName);
                foreach (ConfigNode kerbalNode in kerbalNodes)
                {
                    if (kerbalNode.HasValue("kerbalName"))
                    {
                        string id = kerbalNode.GetValue("kerbalName");
                        this.Log_Debug("DFGameSettings Loading kerbal = " + id);
                        KerbalInfo kerbalInfo = KerbalInfo.Load(kerbalNode);
                        KnownFrozenKerbals[id] = kerbalInfo;
                    }
                }
            }
            this.Log_Debug("DFGameSettings Loading Complete");
        }

        public void Save(ConfigNode node)
        {
            ConfigNode settingsNode;
            if (node.HasNode(configNodeName))
            {
                settingsNode = node.GetNode(configNodeName);
            }
            else
            {
                settingsNode = node.AddNode(configNodeName);
            }

            settingsNode.AddValue("Enabled", Enabled);

            foreach (var entry in KnownFrozenKerbals)
            {
                ConfigNode vesselNode = entry.Value.Save(settingsNode);
                this.Log_Debug("DFGameSettings Saving kerbal = " + entry.Key);
                vesselNode.AddValue("kerbalName", entry.Key);
            }
            this.Log_Debug("DFGameSettings Saving Complete");
        }

        public void DmpKnownFznKerbals()
        {
            this.Log_Debug("Dump of KnownFrozenKerbals");
            if (KnownFrozenKerbals.Count() == 0)
            {
                this.Log_Debug("KnownFrozenKerbals is EMPTY.");
            }
            else
            {
                foreach (KeyValuePair<string, KerbalInfo> kerbal in KnownFrozenKerbals)
                {
                    this.Log_Debug("Kerbal = " + kerbal.Key + " status = " + kerbal.Value.status + " type = " + kerbal.Value.type + " vesselID = " + kerbal.Value.vesselID);

                }
            }
            
        }
    }
}
    