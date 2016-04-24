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
    /// The Wrapper class to access Texture Replacer
    /// </summary>
    public class TRWrapper
    {
        protected static Type TRType;
        protected static Type TRPersonaliserType;

        protected static Object actualTR;
        protected static Object actualTRPersonaliser;

        /// <summary>
        /// This is the Texture Replacer Personaliser object
        ///
        /// SET AFTER INIT
        /// </summary>
        public static TRPersonaliserAPI TexRepPersonaliser;

        /// <summary>
        /// Whether we found the Texture Replacer assembly in the loadedassemblies.
        ///
        /// SET AFTER INIT
        /// </summary>
        public static Boolean AssemblyExists { get { return TRType != null; } }

        /// <summary>
        /// Whether we managed to hook the running Instance from the assembly.
        ///
        /// SET AFTER INIT
        /// </summary>
        public static Boolean InstanceExists { get { return TexRepPersonaliser != null; } }

        /// <summary>
        /// Whether we managed to wrap all the methods/functions from the instance.
        ///
        /// SET AFTER INIT
        /// </summary>
        private static Boolean _TRWrapped;

        /// <summary>
        /// Whether the object has been wrapped
        /// </summary>
        public static Boolean APIReady { get { return _TRWrapped; } }

        /// <summary>
        /// This method will set up the Texture Replacer object and wrap all the methods/functions
        /// </summary>
        /// <param name="Force">This option will force the Init function to rebind everything</param>
        /// <returns></returns>
        public static Boolean InitTRWrapper()
        {
            //reset the internal objects
            _TRWrapped = false;
            actualTR = null;
            TexRepPersonaliser = null;
            LogFormatted_DebugOnly("Attempting to Grab TextureReplacer Types...");

            //find the base type
            TRType = AssemblyLoader.loadedAssemblies
                .Select(a => a.assembly.GetExportedTypes())
                .SelectMany(t => t)
                .FirstOrDefault(t => t.FullName == "TextureReplacer.TextureReplacer");

            if (TRType == null)
            {
                return false;
            }

            LogFormatted("TextureReplacer Version:{0}", TRType.Assembly.GetName().Version.ToString());

            //find the personaliser class type
            TRPersonaliserType = AssemblyLoader.loadedAssemblies
                .Select(a => a.assembly.GetExportedTypes())
                .SelectMany(t => t)
                .FirstOrDefault(t => t.FullName == "TextureReplacer.Personaliser");

            if (TRPersonaliserType == null)
            {
                return false;
            }

            //now grab the running instance
            LogFormatted_DebugOnly("Got Assembly Types, grabbing Instances");
            try
            {
                actualTR = TRType.GetField("isInitialised", BindingFlags.Public | BindingFlags.Static).GetValue(null);
            }
            catch (Exception)
            {
                LogFormatted("No Texture Replacer isInitialised found");
                //throw;
            }
            try
            {
                actualTRPersonaliser = TRPersonaliserType.GetField("instance", BindingFlags.Public | BindingFlags.Static).GetValue(null);
            }
            catch (Exception)
            {
                LogFormatted("No Texture Replacer Personaliser Instance found");
                //throw;
            }
            if (actualTR == null || actualTRPersonaliser == null)
            {
                LogFormatted("Failed grabbing Instance");
                return false;
            }

            //If we get this far we can set up the local object and its methods/functions
            LogFormatted_DebugOnly("Got Instance, Creating Wrapper Objects");
            TexRepPersonaliser = new TRPersonaliserAPI(actualTRPersonaliser);

            _TRWrapped = true;
            return true;
        }

        /// <summary>
        /// The Type that is an analogue of the real Texture replacer. This lets you access all the API-able properties and Methods of Texture Replacer
        /// </summary>
        public class TRPersonaliserAPI
        {
            internal TRPersonaliserAPI(Object TexRepPersonaliser)
            {
                //store the actual object
                APIactualTRPersonaliser = TexRepPersonaliser;

                //these sections get and store the reflection info and actual objects where required. Later in the properties we then read the values from the actual objects
                //for events we also add a handler
                //LogFormatted("Getting APIReady Object");
                //APIReadyField = TRType.GetField("APIReady", BindingFlags.Public | BindingFlags.Static);
                //LogFormatted("Success: " + (APIReadyField != null).ToString());

                //WORK OUT THE STUFF WE NEED TO HOOK FOR PEOPLE HERE
                //Methods
                LogFormatted_DebugOnly("Getting personalise Method");
                personaliseIvaMethod = TRPersonaliserType.GetMethod("personaliseIva", BindingFlags.Public | BindingFlags.Instance);
                LogFormatted_DebugOnly("Success: " + (personaliseIvaMethod != null));
            }

            private Object APIactualTRPersonaliser;

            #region Methods

            private MethodInfo personaliseIvaMethod;

            /// <summary>
            /// Personalise IVA textures of a kerbal
            /// </summary>
            /// <param name="kerbal">The Kerbal reference</param>
            /// <returns>Success of call</returns>
            internal void personaliseIva(Kerbal kerbal)
            {
                try
                {
                    personaliseIvaMethod.Invoke(APIactualTRPersonaliser, new Object[] { kerbal });
                }
                catch (Exception ex)
                {
                    LogFormatted("Unable to invoke Texture Replacer personaliseIva Method");
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