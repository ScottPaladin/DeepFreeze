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
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Debug = UnityEngine.Debug;

namespace DF
{
    /// <summary>
    /// The Wrapper class to access KB LS
    /// </summary>
    public class KBWrapper
    {
        protected static Type KBType;
        protected static Object actualKB;

        /// <summary>
        /// This is the KB LS Actual object
        ///
        /// SET AFTER INIT
        /// </summary>
        public static KBAPI KBActualAPI;

        /// <summary>
        /// Whether we found the KB LS assembly in the loadedassemblies.
        ///
        /// SET AFTER INIT
        /// </summary>
        public static Boolean AssemblyExists { get { return KBType != null; } }

        /// <summary>
        /// Whether we managed to hook the running Instance from the assembly.
        ///
        /// SET AFTER INIT
        /// </summary>
        public static Boolean InstanceExists { get { return KBActualAPI != null; } }

        /// <summary>
        /// Whether we managed to wrap all the methods/functions from the instance.
        ///
        /// SET AFTER INIT
        /// </summary>
        private static Boolean _KBWrapped;

        /// <summary>
        /// Whether the object has been wrapped
        /// </summary>
        public static Boolean APIReady { get { return _KBWrapped; } }

        /// <summary>
        /// This method will set up the KB LS object and wrap all the methods/functions
        /// </summary>
        /// <returns></returns>
        public static Boolean InitKBWrapper()
        {
            //reset the internal objects
            _KBWrapped = false;
            actualKB = null;
            LogFormatted_DebugOnly("Attempting to Grab KB LS Types...");

            //find the base type
            KBType = getType("KERBALISM.Kerbalism");

            if (KBType == null)
            {
                return false;
            }

            LogFormatted("KB LS Version:{0}", KBType.Assembly.GetName().Version.ToString());
            
            //If we get this far we can set up the local object and its methods/functions
            LogFormatted_DebugOnly("Got Instance, Creating Wrapper Objects");
            KBActualAPI = new KBAPI();
            //KBIsKerbalTracked = new KBIsKerbalTrackedAPI(actualKBIsKerbalTracked);

            _KBWrapped = true;
            return true;
        }

        internal static Type getType(string name)
        {
            Type type = null;
            AssemblyLoader.loadedAssemblies.TypeOperation(t =>

            {
                if (t.FullName == name)
                    type = t;
            }
            );

            if (type != null)
            {
                return type;
            }
            return null;
        }

        /// <summary>
        /// The Type that is an analogue of the real KB LS. This lets you access all the API-able properties and Methods of KB LS
        public class KBAPI
        {
            internal KBAPI()
            {
                //these sections get and store the reflection info and actual objects where required. Later in the properties we then read the values from the actual objects
                //for events we also add a handler
                //LogFormatted("Getting APIReady Object");
                //APIReadyField = TRType.GetField("APIReady", BindingFlags.Public | BindingFlags.Static);
                //LogFormatted("Success: " + (APIReadyField != null).ToString());

                //WORK OUT THE STUFF WE NEED TO HOOK FOR PEOPLE HERE
                //Methods
                LogFormatted_DebugOnly("Getting hook_DisableKerbal Method");
                KBhook_DisableKerbalMethod = KBType.GetMethod("hook_DisableKerbal", BindingFlags.Public | BindingFlags.Static);
                LogFormatted_DebugOnly("Success: " + (KBhook_DisableKerbalMethod != null));
            }
            
            #region Methods

            private MethodInfo KBhook_DisableKerbalMethod;

            /// <summary>
            /// Un/track a kerbal in KB LS
            /// </summary>
            /// <param name="kerbal">A string containing the kerbal's name</param>
            /// <param name="disabled">A bool to disable or enable</param>
            internal void DisableKerbal(string kerbal, bool disabled)
            {
                try
                {
                    KBhook_DisableKerbalMethod.Invoke(null, new System.Object[] { kerbal, disabled });
                }
                catch (Exception ex)
                {
                    LogFormatted("Unable to invoke KB LS DisableKerbal Method");
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
        internal static void LogFormatted_DebugOnly(String Message, params Object[] strParams)
        {
            if (RSTUtils.Utilities.debuggingOn)
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
                DateTime.Now, Message, Assembly.GetExecutingAssembly().GetName().Name,
                MethodBase.GetCurrentMethod().DeclaringType.Name);
            Debug.Log(strMessageLine);
        }

        #endregion Logging Stuff
    }
}