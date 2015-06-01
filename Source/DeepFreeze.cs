/**
 * DeepFreezerPart.cs
 *
 *
 * Kerbal Space Program is Copyright (C) 2013 Squad. See http://kerbalspaceprogram.com/. This
 * project is in no way associated with nor endorsed by Squad.
 *
 *  This file is part of Jamie Leighton's Fork of DeepFreeze. Original Author of DeepFreeze is 'scottpaladin' on the KSP Forums.
 *  The original DeepFreeze was licensed under the Attribution-NonCommercial-ShareAlike 3.0 (CC BY-NC-SA 4.0)
 *  This File was not part of the original Deepfreeze and was written by Jamie Leighton.
 *  (C) Copyright 2015, Jamie Leighton
 *
 * Which is licensed under the Attribution-NonCommercial-ShareAlike 3.0 (CC BY-NC-SA 4.0)
 * creative commons license. See <https://creativecommons.org/licenses/by-nc-sa/4.0/>
 * for full details.
 *
 */

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DF
{
    [KSPAddon(KSPAddon.Startup.EveryScene, false)]
    internal class DeepFreezeEvnts : MonoBehaviour
    {
        

        public void Start()
        {
            Debug.Log("DeepFreezeEvnts.Start called");
            if (!DeepFreezeEvents.instance.eventAdded)
            {
                DeepFreezeEvents.instance.DeepFreezeEventAdd();
                Debug.Log("!DeepFreezeEvents.instance.eventAdded");
            }
            else
                Debug.Log("DeepFreezeEvents.instance.eventAdded");
        }
    }

    [KSPAddon(KSPAddon.Startup.SpaceCentre, false)]
    public class AddScenarioModules : MonoBehaviour
    {
        private void Start()
        {
            var Currentgame = HighLogic.CurrentGame;
            Utilities.Log("DeepFreeze  AddScenarioModules", " ScenarioModules Start");
            ProtoScenarioModule protoscenmod = Currentgame.scenarios.Find(s => s.moduleName == typeof(DeepFreeze).Name);

            if (protoscenmod == null)
            {
                Utilities.Log("DeepFreeze AddScenarioModules", " Adding the scenario module.");
                protoscenmod = Currentgame.AddProtoScenarioModule(typeof(DeepFreeze), GameScenes.SPACECENTER, GameScenes.FLIGHT, GameScenes.EDITOR, GameScenes.TRACKSTATION);
            }
            else
            {
                if (!protoscenmod.targetScenes.Any(s => s == GameScenes.SPACECENTER))
                {
                    Utilities.Log("DeepFreeze  AddScenarioModules", " Adding the SpaceCenter scenario module.");
                    protoscenmod.targetScenes.Add(GameScenes.SPACECENTER);
                }
                if (!protoscenmod.targetScenes.Any(s => s == GameScenes.FLIGHT))
                {
                    Utilities.Log("DeepFreeze  AddScenarioModules", " Adding the flight scenario module.");
                    protoscenmod.targetScenes.Add(GameScenes.FLIGHT);
                }
                if (!protoscenmod.targetScenes.Any(s => s == GameScenes.EDITOR))
                {
                    Utilities.Log("DeepFreeze  AddScenarioModules", " Adding the Editor scenario module.");
                    protoscenmod.targetScenes.Add(GameScenes.EDITOR);
                }
                if (!protoscenmod.targetScenes.Any(s => s == GameScenes.TRACKSTATION))
                {
                    Utilities.Log("DeepFreeze  AddScenarioModules", " Adding the Editor scenario module.");
                    protoscenmod.targetScenes.Add(GameScenes.TRACKSTATION);
                }
            }
        }
    }

    public class DeepFreeze : ScenarioModule
    {
        public static DeepFreeze Instance { get; private set; }
        public DFSettings DFsettings { get; private set; }        

        private readonly string globalConfigFilename;

        //private readonly string FilePath;
        private ConfigNode globalNode = new ConfigNode();

        private readonly List<Component> children = new List<Component>();

        private static bool? _SMInstalled = null;
        public bool SMInstalled
        {
            get
            {
                return (bool)_SMInstalled;
            }
        }

        public DeepFreeze()
        {
            Utilities.Log("DeepFreeze", "Constructor");
            Instance = this;
            DFsettings = new DFSettings();            

            globalConfigFilename = System.IO.Path.Combine(_AssemblyFolder, "Config.cfg").Replace("\\", "/");
            this.Log("globalConfigFilename = " + globalConfigFilename);

            _SMInstalled = ShipManifest.SMInterface.IsSMInstalled;
            if (SMInstalled) Debug.Log("ShipManifest is Installed");
            else Debug.Log("ShipManifest is NOT Installed");     
        }

        public override void OnAwake()
        {
            this.Log("OnAwake in " + HighLogic.LoadedScene);
            base.OnAwake();

            if (HighLogic.LoadedScene == GameScenes.SPACECENTER)
            {
                this.Log("Adding SpaceCenterManager");
                var child = gameObject.AddComponent<FrozenKerbals>();
                children.Add(child);
            }
            else if (HighLogic.LoadedScene == GameScenes.FLIGHT)
            {
                this.Log("Adding FlightManager");
                var child = gameObject.AddComponent<FrozenKerbals>();
                children.Add(child);
            }
            else if (HighLogic.LoadedScene == GameScenes.EDITOR)
            {
                this.Log("Adding EditorController");
                var child = gameObject.AddComponent<FrozenKerbals>();
                children.Add(child);
            }
            else if (HighLogic.LoadedScene == GameScenes.TRACKSTATION)
            {
                this.Log("Adding TrackingStationController");
                var child = gameObject.AddComponent<FrozenKerbals>();
                children.Add(child);
            }
        }

        public override void OnLoad(ConfigNode gameNode)
        {
            base.OnLoad(gameNode);            

            // Load the global settings
            if (System.IO.File.Exists(globalConfigFilename))
            {
                globalNode = ConfigNode.Load(globalConfigFilename);
                DFsettings.Load(globalNode);
                foreach (Savable s in children.Where(c => c is Savable))
                {
                    this.Log("DeepFreeze Child Load Call for " + s.ToString());
                    s.Load(globalNode);
                }
            }
            this.Log("OnLoad: \n " + gameNode + "\n" + globalNode);
        }

        public override void OnSave(ConfigNode gameNode)
        {
            base.OnSave(gameNode);            
            foreach (Savable s in children.Where(c => c is Savable))
            {
                this.Log("DeepFreeze Child Save Call for " + s.ToString());
                s.Save(globalNode);
            }
            DFsettings.Save(globalNode);
            globalNode.Save(globalConfigFilename);

            this.Log("OnSave: " + gameNode + "\n" + globalNode);
        }

        private void OnDestroy()
        {
            this.Log("OnDestroy");
            foreach (Component child in children)
            {
                this.Log("DeepFreeze Child Destroy for " + child.name);
                Destroy(child);
            }
            children.Clear();
        }

        #region Assembly/Class Information

        /// <summary>
        /// Name of the Assembly that is running this MonoBehaviour
        /// </summary>
        internal static String _AssemblyName
        { get { return System.Reflection.Assembly.GetExecutingAssembly().GetName().Name; } }

        /// <summary>
        /// Full Path of the executing Assembly
        /// </summary>
        internal static String _AssemblyLocation
        { get { return System.Reflection.Assembly.GetExecutingAssembly().Location; } }

        /// <summary>
        /// Folder containing the executing Assembly
        /// </summary>
        internal static String _AssemblyFolder
        { get { return System.IO.Path.GetDirectoryName(_AssemblyLocation); } }

        #endregion Assembly/Class Information
    }

    internal interface Savable
    {
        void Load(ConfigNode globalNode);

        void Save(ConfigNode globalNode);
    }
}