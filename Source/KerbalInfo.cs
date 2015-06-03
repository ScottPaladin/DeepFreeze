using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DF
{
    public class KerbalInfo
    {
        public const string ConfigNodeName = "KerbalInfo";
                
        public double lastUpdate = 0f;
        public ProtoCrewMember.RosterStatus status;
        public ProtoCrewMember.KerbalType type;
        public Guid vesselID;
        public int partID;
        public int seatIdx;
        public string seatName;
        public string experienceTraitName;

           
        public KerbalInfo(double currentTime)
        {                    
            lastUpdate = currentTime;                       
        }

        public static KerbalInfo Load(ConfigNode node)
        {
            
            double lastUpdate = Utilities.GetNodeValue(node, "lastUpdate", 0.0);

            KerbalInfo info = new KerbalInfo(lastUpdate);
            info.status = Utilities.GetNodeValue(node, "status", ProtoCrewMember.RosterStatus.Dead);
            info.type = Utilities.GetNodeValue(node, "type", ProtoCrewMember.KerbalType.Unowned);
            string tmpvesselID = Utilities.GetNodeValue(node, "vesselID", "");
            Utilities.Log_Debug("DeepFreeze", "Value of VesselID nodevalue = " + tmpvesselID);

            try
            {
                info.vesselID = new Guid(tmpvesselID);
            }
            catch (Exception ex)
            {
                info.vesselID = Guid.Empty;
                Utilities.Log("DeepFreeze", "Load of GUID VesselID for frozen kerbal failed Err: " + ex);                
            }
            //info.vesselID = Utilities.GetNodeValue(node, "vesselID");
            info.partID = Utilities.GetNodeValue(node, "partID", 0);
            info.seatIdx = Utilities.GetNodeValue(node, "seatIdx", 0);
            info.seatName = Utilities.GetNodeValue(node, "seatName", "");
            info.experienceTraitName = Utilities.GetNodeValue(node, "experienceTraitName", "");       
                                 
            return info;
        }

        public ConfigNode Save(ConfigNode config)
        {
            ConfigNode node = config.AddNode(ConfigNodeName);            
            node.AddValue("lastUpdate", lastUpdate);            
            node.AddValue("status", status);
            node.AddValue("type", type);
            node.AddValue("vesselID", vesselID);
            node.AddValue("partID", partID);
            node.AddValue("seatIdx", seatIdx);
            node.AddValue("seatName", seatName);
            node.AddValue("experienceTraitName", experienceTraitName);

            return node;
        }
                
    }
}
