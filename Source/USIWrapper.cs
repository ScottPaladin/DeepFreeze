/**
 * DeepFreeze Continued...
 * (C) Copyright 2015, Jamie Leighton
 *
 * Kerbal Space Program is Copyright (C) 2013 Squad. See http://kerbalspaceprogram.com/. This
 * project is in no way associated with nor endorsed by Squad.
 *
 *  This file is part of JPLRepo's DeepFreeze (continued...) - a Fork of DeepFreeze. Original Author of DeepFreeze is 'scottpaladin' on the KSP Forums.
 *  This File was not part of the original Deepfreeze but was written by Jamie Leighton based of code and concepts from the Kerbal Alarm Clock Mod. Which was licensed under the MIT license.
 *  (C) Copyright 2015, Jamie Leighton
 *
 * Continues to be licensed under the Attribution-NonCommercial-ShareAlike 3.0 (CC BY-NC-SA 4.0)
 * creative commons license. See <https://creativecommons.org/licenses/by-nc-sa/4.0/>
 * for full details.
 *
 */
 using System;
using System.Linq;
using System.Reflection;

namespace DF
{
    /// <summary>
    /// The Wrapper class to access USI LS
    /// </summary>
    public class USIWrapper
    {
        protected static System.Type USIType;
        protected static System.Type USIUntrackKerbalType;

        protected static Object actualUSI = null;
        protected static Object actualUSIUntrackKerbal = null;

        /// <summary>
        /// This is the USI LS UntrackKerbal object
        ///
        /// SET AFTER INIT
        /// </summary>
        public static USIUntrackKerbalAPI USIUntrackKerbal = null;

        /// <summary>
        /// Whether we found the USI LS assembly in the loadedassemblies.
        ///
        /// SET AFTER INIT
        /// </summary>
        public static Boolean AssemblyExists { get { return (USIType != null); } }

        /// <summary>
        /// Whether we managed to hook the running Instance from the assembly.
        ///
        /// SET AFTER INIT
        /// </summary>
        public static Boolean InstanceExists { get { return (USIUntrackKerbal != null); } }

        /// <summary>
        /// Whether we managed to wrap all the methods/functions from the instance.
        ///
        /// SET AFTER INIT
        /// </summary>
        private static Boolean _USIWrapped = false;

        /// <summary>
        /// Whether the object has been wrapped
        /// </summary>
        public static Boolean APIReady { get { return _USIWrapped; } }

        /// <summary>
        /// This method will set up the USI LS object and wrap all the methods/functions
        /// </summary>
        /// <param name="Force">This option will force the Init function to rebind everything</param>
        /// <returns></returns>
        public static Boolean InitUSIWrapper()
        {
            //reset the internal objects
            _USIWrapped = false;
            actualUSI = null;
            USIUntrackKerbal = null;
            LogFormatted("Attempting to Grab USI LS Types...");

            //find the base type
            USIType = AssemblyLoader.loadedAssemblies
                .Select(a => a.assembly.GetExportedTypes())
                .SelectMany(t => t)
                .FirstOrDefault(t => t.FullName == "LifeSupport.LifeSupportScenario");

            if (USIType == null)
            {
                return false;
            }

            LogFormatted("USI LS Version:{0}", USIType.Assembly.GetName().Version.ToString());

            //find the LifeSupportManager class type
            USIUntrackKerbalType = AssemblyLoader.loadedAssemblies
                .Select(a => a.assembly.GetExportedTypes())
                .SelectMany(t => t)
                .FirstOrDefault(t => t.FullName == "LifeSupport.LifeSupportManager");

            if (USIUntrackKerbalType == null)
            {
                return false;
            }

            //now grab the running instance
            LogFormatted("Got Assembly Types, grabbing Instances");

            try
            {
                actualUSIUntrackKerbal = USIUntrackKerbalType.GetField("instance", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
            }
            catch (Exception)
            {
                LogFormatted("No USI LS UntrackKerbal Instance found");
                //throw;
            }
            //if (actualUSI == null || actualUSIUntrackKerbal == null)
            if (actualUSIUntrackKerbal == null)
            {
                LogFormatted("Failed grabbing Instance");
                return false;
            }

            //If we get this far we can set up the local object and its methods/functions
            LogFormatted("Got Instance, Creating Wrapper Objects");
            USIUntrackKerbal = new USIUntrackKerbalAPI(actualUSIUntrackKerbal);

            _USIWrapped = true;
            return true;
        }

        /// <summary>
        /// The Type that is an analogue of the real USI LS. This lets you access all the API-able properties and Methods of USI LS
        public class USIUntrackKerbalAPI
        {
            internal USIUntrackKerbalAPI(Object USIUntrackKerbal)
            {
                //store the actual object
                APIactualUSIUntrackKerbal = USIUntrackKerbal;

                //these sections get and store the reflection info and actual objects where required. Later in the properties we then read the values from the actual objects
                //for events we also add a handler
                //LogFormatted("Getting APIReady Object");
                //APIReadyField = TRType.GetField("APIReady", BindingFlags.Public | BindingFlags.Static);
                //LogFormatted("Success: " + (APIReadyField != null).ToString());

                //WORK OUT THE STUFF WE NEED TO HOOK FOR PEOPLE HERE
                //Methods
                LogFormatted("Getting UntrackKerbal Method");
                USIUntrackKerbalMethod = USIUntrackKerbalType.GetMethod("UntrackKerbal", BindingFlags.Public | BindingFlags.Instance);
                LogFormatted_DebugOnly("Success: " + (USIUntrackKerbalMethod != null).ToString());
            }

            private Object APIactualUSIUntrackKerbal;

            #region Methods

            private MethodInfo USIUntrackKerbalMethod;

            /// <summary>
            /// Untrack a kerbal in USI LS
            /// </summary>
            /// <param name="kerbal">A string containing the kerbal's name</param>            
            internal void UntrackKerbal(string kerbal)
            {
                try
                {
                    USIUntrackKerbalMethod.Invoke(APIactualUSIUntrackKerbal, new System.Object[] { kerbal });
                }
                catch (Exception ex)
                {
                    LogFormatted("Unable to invoke USI LS UntrackKerbal Method");
                    LogFormatted("Exception: {0}", ex);
                    //throw;
                }
            }

            #endregion Methods
        }

        #region Logging Stuff

        /// <summary>
        /// Some Structured logging to the debug file - ONLY RUNS WHEN DLL COMPILED IN DEBUG MODE
        /// </summary>
        /// <param name="Message">Text to be printed - can be formatted as per String.format</param>
        /// <param name="strParams">Objects to feed into a String.format</param>
        [System.Diagnostics.Conditional("DEBUG")]
        internal static void LogFormatted_DebugOnly(String Message, params Object[] strParams)
        {
            LogFormatted(Message, strParams);
        }

        /// <summary>
        /// Some Structured logging to the debug file
        /// </summary>
        /// <param name="Message">Text to be printed - can be formatted as per String.format</param>
        /// <param name="strParams">Objects to feed into a String.format</param>
        internal static void LogFormatted(String Message, params Object[] strParams)
        {
            Message = String.Format(Message, strParams);
            String strMessageLine = String.Format("{0},{2}-{3},{1}",
                DateTime.Now, Message, System.Reflection.Assembly.GetExecutingAssembly().GetName().Name,
                System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name);
            UnityEngine.Debug.Log(strMessageLine);
        }

        #endregion Logging Stuff
    }
}