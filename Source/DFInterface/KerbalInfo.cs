using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace DF
{
    public class KerbalInfo
    {
        public const string ConfigNodeName = "KerbalInfo";
                
        public double lastUpdate = 0f;
        public ProtoCrewMember.RosterStatus status;
        public ProtoCrewMember.KerbalType type;
        public Guid vesselID;
        public uint partID;
        public int seatIdx;
        public string seatName;
        public string experienceTraitName;

           
        public KerbalInfo(double currentTime)
        {                    
            lastUpdate = currentTime;                       
        }

        public static KerbalInfo Load(ConfigNode node)
        {
            
            double lastUpdate = GetNodeValue(node, "lastUpdate", 0.0);

            KerbalInfo info = new KerbalInfo(lastUpdate);
            info.status = GetNodeValue(node, "status", ProtoCrewMember.RosterStatus.Dead);
            info.type = GetNodeValue(node, "type", ProtoCrewMember.KerbalType.Unowned);
            string tmpvesselID = GetNodeValue(node, "vesselID", "");            

            try
            {
                info.vesselID = new Guid(tmpvesselID);
            }
            catch (Exception ex)
            {
                info.vesselID = Guid.Empty;
               Debug.Log("DFInterface - Load of GUID VesselID for frozen kerbal failed Err: " + ex);                
            }
            info.partID = GetNodeValue(node, "partID", (uint)0);
            info.seatIdx = GetNodeValue(node, "seatIdx", 0);
            info.seatName = GetNodeValue(node, "seatName", "");
            info.experienceTraitName = GetNodeValue(node, "experienceTraitName", "");       
                                 
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
                
        public static int GetNodeValue(ConfigNode confignode, string fieldname, int defaultValue)
        {
            int newValue;
            if (confignode.HasValue(fieldname) && int.TryParse(confignode.GetValue(fieldname), out newValue))
            {
                return newValue;
            }
            else
            {
                return defaultValue;
            }
        }

        public static uint GetNodeValue(ConfigNode confignode, string fieldname, uint defaultValue)
        {
            uint newValue;
            if (confignode.HasValue(fieldname) && uint.TryParse(confignode.GetValue(fieldname), out newValue))
            {
                return newValue;
            }
            else
            {
                return defaultValue;
            }
        }
        
        public static double GetNodeValue(ConfigNode confignode, string fieldname, double defaultValue)
        {
            double newValue;
            if (confignode.HasValue(fieldname) && double.TryParse(confignode.GetValue(fieldname), out newValue))
            {
                return newValue;
            }
            else
            {
                return defaultValue;
            }
        }

        public static string GetNodeValue(ConfigNode confignode, string fieldname, string defaultValue)
        {
            if (confignode.HasValue(fieldname))
            {
                return confignode.GetValue(fieldname);
            }
            else
            {
                return defaultValue;
            }
        }

        public static Guid GetNodeValue(ConfigNode confignode, string fieldname)
        {
            if (confignode.HasValue(fieldname))
            {
                confignode.GetValue(fieldname);                
                return new Guid(fieldname);
            }
            else
            {

                return Guid.Empty;
            }
        }

        public static T GetNodeValue<T>(ConfigNode confignode, string fieldname, T defaultValue) where T : IComparable, IFormattable, IConvertible
        {
            if (confignode.HasValue(fieldname))
            {
                string stringValue = confignode.GetValue(fieldname);
                if (Enum.IsDefined(typeof(T), stringValue))
                {
                    return (T)Enum.Parse(typeof(T), stringValue);
                }
            }
            return defaultValue;
        }
                
    }
}
