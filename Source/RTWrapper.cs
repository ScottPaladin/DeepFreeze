using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace DF
{
    /// <summary>
    /// The Wrapper class to access Texture Replacer
    /// </summary>
    public class RTWrapper
    {
        protected static System.Type RTAPIType;        
        protected static Object actualRTAPI = null;        

        /// <summary>
        /// This is the Remote Tech API object
        ///
        /// SET AFTER INIT
        /// </summary>
        public static RTAPI RTactualAPI = null;

        /// <summary>
        /// Whether we found the Remote Tech API assembly in the loadedassemblies.
        ///
        /// SET AFTER INIT
        /// </summary>
        public static Boolean AssemblyExists { get { return (RTAPIType != null); } }

        /// <summary>
        /// Whether we managed to hook the running Instance from the assembly.
        ///
        /// SET AFTER INIT
        /// </summary>
        public static Boolean InstanceExists { get { return (actualRTAPI != null); } }

        /// <summary>
        /// Whether we managed to wrap all the methods/functions from the instance.
        ///
        /// SET AFTER INIT
        /// </summary>
        private static Boolean _RTWrapped = false;

        /// <summary>
        /// Whether the object has been wrapped 
        /// </summary>
        public static Boolean APIReady { get { return _RTWrapped; } }

        /// <summary>
        /// This method will set up the Texture Replacer object and wrap all the methods/functions
        /// </summary>
        /// <param name="Force">This option will force the Init function to rebind everything</param>
        /// <returns></returns>
        public static Boolean InitTRWrapper()
        {
            //reset the internal objects
            _RTWrapped = false;
            actualRTAPI = null;            
            LogFormatted("Attempting to Grab TextureReplacer Types...");

            
            //find the base type
            RTAPIType = AssemblyLoader.loadedAssemblies
                .Select(a => a.assembly.GetExportedTypes())
                .SelectMany(t => t)
                .FirstOrDefault(t => t.FullName == "RemoteTech.API.API");

            if (RTAPIType == null)
            {
                return false;
            }

            LogFormatted("Remote Tech Version:{0}", RTAPIType.Assembly.GetName().Version.ToString());
                        
            //now grab the running instance
            LogFormatted("Got Assembly Types, grabbing Instances");
            try
            {
                actualRTAPI = RTAPIType.GetMember("HasLocalControl", BindingFlags.Public | BindingFlags.Static);
            }
            catch (Exception)
            {
                LogFormatted("No Texture Replacer isInitialised found");
                //throw;
            }
            
            if (actualRTAPI == null)
            {
                LogFormatted("Failed grabbing Instance");
                return false;
            }

            //If we get this far we can set up the local object and its methods/functions
            LogFormatted("Got Instance, Creating Wrapper Objects");
            RTactualAPI = new RTAPI(actualRTAPI);

            _RTWrapped = true;
            return true;
        }

        /// <summary>
        /// The Type that is an analogue of the real KAC. This lets you access all the API-able properties and Methods of the KAC
        /// </summary>
        public class RTAPI
        {
            internal RTAPI(Object actualRT)
            {
                //store the actual object
                APIactualRT = actualRT;

                //these sections get and store the reflection info and actual objects where required. Later in the properties we then read the values from the actual objects
                //for events we also add a handler
                
                //WORK OUT THE STUFF WE NEED TO HOOK FOR PEOPLE HERE
                //Methods
                LogFormatted("Getting HasLocalControl Method");
                HasLocalControlMethod = RTAPIType.GetMethod("HasLocalControl", BindingFlags.Public | BindingFlags.Static);
                LogFormatted_DebugOnly("Success: " + (HasLocalControlMethod != null).ToString());

                LogFormatted("Getting HasAnyConnection Method");
                HasAnyConnectionMethod = RTAPIType.GetMethod("HasAnyConnection", BindingFlags.Public | BindingFlags.Static);
                LogFormatted_DebugOnly("Success: " + (HasAnyConnectionMethod != null).ToString());

                LogFormatted("Getting GetShortestSignalDelay Method");
                GetShortestSignalDelayMethod = RTAPIType.GetMethod("GetShortestSignalDelay", BindingFlags.Public | BindingFlags.Static);
                LogFormatted_DebugOnly("Success: " + (GetShortestSignalDelayMethod != null).ToString());

            }

            private Object APIactualRT;

            #region Methods

            private MethodInfo HasLocalControlMethod;

            /// <summary>
            /// Personalise IVA textures of a kerbal
            /// </summary>
            /// <param name="kerbal">The Kerbal reference</param>
            /// <returns>Success of call</returns>
            internal bool HasLocalControl(Guid id)
            {
                try
                {
                    return (bool)HasLocalControlMethod.Invoke(APIactualRT, new System.Object[] { id });
                }
                catch (Exception ex)
                {
                    LogFormatted("Unable to invoke Remote Tech HasLocalControl Method");
                    LogFormatted("Exception: {0}", ex);
                    return false;
                    //throw;
                }

            }

            private MethodInfo HasAnyConnectionMethod;

            /// <summary>
            /// Personalise IVA textures of a kerbal
            /// </summary>
            /// <param name="kerbal">The Kerbal reference</param>
            /// <returns>Success of call</returns>
            internal bool HasAnyConnection(Guid id)
            {
                try
                {
                    return (bool)HasAnyConnectionMethod.Invoke(APIactualRT, new System.Object[] { id });
                }
                catch (Exception ex)
                {
                    LogFormatted("Unable to invoke Remote Tech HasAnyConnection Method");
                    LogFormatted("Exception: {0}", ex);
                    return false;
                    //throw;
                }

            }

            private MethodInfo GetShortestSignalDelayMethod;

            /// <summary>
            /// Personalise IVA textures of a kerbal
            /// </summary>
            /// <param name="kerbal">The Kerbal reference</param>
            /// <returns>Success of call</returns>
            internal double GetShortestSignalDelay(Guid id)
            {
                try
                {
                    return (double)GetShortestSignalDelayMethod.Invoke(APIactualRT, new System.Object[] { id });
                }
                catch (Exception ex)
                {
                    LogFormatted("Unable to invoke Remote Tech GetShortestSignalDelay Method");
                    LogFormatted("Exception: {0}", ex);
                    return 0;
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