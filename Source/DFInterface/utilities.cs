/**
 * Utilities.cs
 *
 *
 * Kerbal Space Program is Copyright (C) 2013 Squad. See http://kerbalspaceprogram.com/. This
 * project is in no way associated with nor endorsed by Squad.
 *
 *  This file is part of Jamie Leighton's Fork of DeepFreeze. Original Author of DeepFreeze is 'scottpaladin' on the KSP Forums.
 *  This File was not part of the original Deepfreeze but was written by Jamie Leighton.
 *  (C) Copyright 2015, Jamie Leighton
 *
 * Which is licensed under the Attribution-NonCommercial-ShareAlike 3.0 (CC BY-NC-SA 4.0)
 * creative commons license. See <https://creativecommons.org/licenses/by-nc-sa/4.0/>
 * for full details.
 *
 */

using System;
using UnityEngine;

namespace DF
{
    public static class Utilities
    {
        // GUI & Window Methods

        public static bool WindowVisibile(Rect winpos)
        {
            float minmargin = 20.0f; // 20 bytes margin for the window
            float xMin = minmargin - winpos.width;
            float xMax = Screen.width - minmargin;
            float yMin = minmargin - winpos.height;
            float yMax = Screen.height - minmargin;
            bool xRnge = (winpos.x > xMin) && (winpos.x < xMax);
            bool yRnge = (winpos.y > yMin) && (winpos.y < yMax);
            return xRnge && yRnge;
        }

        public static Rect MakeWindowVisible(Rect winpos)
        {
            float minmargin = 20.0f; // 20 bytes margin for the window
            float xMin = minmargin - winpos.width;
            float xMax = Screen.width - minmargin;
            float yMin = minmargin - winpos.height;
            float yMax = Screen.height - minmargin;

            winpos.x = Mathf.Clamp(winpos.x, xMin, xMax);
            winpos.y = Mathf.Clamp(winpos.y, yMin, yMax);

            return winpos;
        }

        // Get Config Node Values out of a config node Methods

        public static bool GetNodeValue(ConfigNode confignode, string fieldname, bool defaultValue)
        {
            bool newValue;
            if (confignode.HasValue(fieldname) && bool.TryParse(confignode.GetValue(fieldname), out newValue))
            {
                return newValue;
            }
            else
            {
                return defaultValue;
            }
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

        public static float GetNodeValue(ConfigNode confignode, string fieldname, float defaultValue)
        {
            float newValue;
            if (confignode.HasValue(fieldname) && float.TryParse(confignode.GetValue(fieldname), out newValue))
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

        public static Guid GetNodeValue(ConfigNode confignode, string fieldname, Guid defaultValue)
        {
            if (confignode.HasValue(fieldname))
            {
                confignode.GetValue(fieldname);
                return new Guid(fieldname);
            }
            else
            {
                
                return defaultValue;
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

        // Logging Functions
        // Name of the Assembly that is running this MonoBehaviour
        internal static String _AssemblyName
        { get { return System.Reflection.Assembly.GetExecutingAssembly().GetName().Name; } }

        public static void Log(this UnityEngine.Object obj, string message)
        {
            Debug.Log(obj.GetType().FullName + "[" + obj.GetInstanceID().ToString("X") + "][" + Time.time.ToString("0.00") + "]: " + message);
        }

        public static void Log(this System.Object obj, string message)
        {
            Debug.Log(obj.GetType().FullName + "[" + obj.GetHashCode().ToString("X") + "][" + Time.time.ToString("0.00") + "]: " + message);
        }

        public static void Log(string context, string message)
        {
            Debug.Log(context + "[][" + Time.time.ToString("0.00") + "]: " + message);
        }        
    }
}