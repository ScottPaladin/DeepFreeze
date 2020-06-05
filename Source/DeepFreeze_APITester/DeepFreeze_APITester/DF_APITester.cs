using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DF_APITester
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class APITester : MonoBehaviour
    {        
        
        private List<PartModule> deepfreezers = new List<PartModule>();

        //Temporary GUI display fields
        private Rect DFwindowPos;
        private static int windowID = 199999;
        private Vector2 GUIscrollViewVector = Vector2.zero;
        private GUIStyle sectionTitleStyle, statusStyle;
        private int seats;
        private bool isxferto;
        private bool isxferfrom;
        private bool isfreezeact;
        private bool isthawact;
        private bool outofec;
        private bool partfull;
        private int freeseats;
        private DFWrapper.FrzrTmpStatus tmpsts;
        private int totfrozen;
        private DFWrapper.FrznCrewList frzncrewlst = new DFWrapper.FrznCrewList();
        private Dictionary<string, DFWrapper.KerbalInfo> DFFrozenKerbals = new Dictionary<string, DFWrapper.KerbalInfo>();

        internal void Start()
        {
            Debug.Log("DFAPITESTER Start");                        
            DFwindowPos = new Rect(40, 40, 250, 400);
            RenderingManager.AddToPostDrawQueue(3, this.onDraw);
        }

        internal void OnDestroy()
        {
            RenderingManager.RemoveFromPostDrawQueue(3, this.onDraw);
        }

        internal void Update()
        {
            if (Time.timeSinceLevelLoad < 2.0f) // Check not loading level
                return;
            
            if (!DFWrapper.InstanceExists)  // Check if DFWrapper has been initialized or not. If not try to initialize.
            {
                DFWrapper.InitDFWrapper();                
            }
            
            if (DFWrapper.APIReady)  // If the DeepFreeze API is Ready we do stuff with it.
            {                
                DFFrozenKerbals.Clear();
                //Get the DeepFreeze Dictionary of all Frozen Kerbals in the current Game.
                DFFrozenKerbals = DFWrapper.DeepFreezeAPI.FrozenKerbals;

                //Go through the active vessel and get all the DeepFreezer partmodules.
                deepfreezers.Clear();                
                if (FlightGlobals.ActiveVessel != null)
                {
                    //Get a List of all the PartModules that are DeepFreezer class and store in deepfreezers List.
                    List<Part> cryofreezers = (from p in FlightGlobals.ActiveVessel.parts where p.Modules.Contains("DeepFreezer") select p).ToList();
                    foreach (Part part in cryofreezers)
                    {
                        PartModule deepFreezer = (from PartModule pm in part.Modules where pm.moduleName == "DeepFreezer" select pm).SingleOrDefault();
                        if (deepFreezer != null)
                        {
                            deepfreezers.Add(deepFreezer);
                        }
                    }

                    //If we found any DeepFreezer partmodules
                    if (deepfreezers.Count == 1)
                    {
                        foreach (PartModule module in deepfreezers)
                        {

                            //The DFWrapper.DeepFreezer class is a reflection class of the real DeepFreezer PartModule
                            DFWrapper.DeepFreezer freezer = new DFWrapper.DeepFreezer(module);

                            //Populate fields for GUI display - NB: This only works for ONE freezer module in active vessel
                            //But is only for example purposes. If you want to do this for multiple modules then
                            // you will have to create your own Lists, etc.
                            seats = freezer.FreezerSize;
                            isxferto = freezer.crewXferTOActive;
                            isxferfrom = freezer.crewXferFROMActive;
                            isfreezeact = freezer.IsFreezeActive;
                            isthawact = freezer.IsThawActive;
                            outofec = freezer.FreezerOutofEC;
                            partfull = freezer.PartFull;
                            freeseats = freezer.FreezerSpace;
                            tmpsts = freezer.FrzrTmp;
                            totfrozen = freezer.TotalFrozen;
                            frzncrewlst = freezer.StoredCrewList;
                        }
                    }
                }
            }                     
        }

        private void onDraw()
        {
            if (HighLogic.LoadedScene == GameScenes.FLIGHT)
            {
                GUI.skin = HighLogic.Skin;
                DFwindowPos = GUILayout.Window(windowID, DFwindowPos, windowDF, "DeepFreeze API Test", GUILayout.ExpandWidth(true),
                        GUILayout.ExpandHeight(true), GUILayout.MinWidth(200), GUILayout.MinHeight(400));
            }
        }

        private void windowDF(int id)
        {
            //Init styles
            sectionTitleStyle = new GUIStyle(GUI.skin.label);
            sectionTitleStyle.alignment = TextAnchor.MiddleLeft;
            sectionTitleStyle.stretchWidth = true;
            sectionTitleStyle.normal.textColor = Color.blue;
            sectionTitleStyle.fontStyle = FontStyle.Bold;

            statusStyle = new GUIStyle(GUI.skin.label);
            statusStyle.alignment = TextAnchor.MiddleLeft;
            statusStyle.stretchWidth = true;
            statusStyle.normal.textColor = Color.white;

            GUILayout.BeginVertical();
            GUIscrollViewVector = GUILayout.BeginScrollView(GUIscrollViewVector, false, false);
            GUILayout.BeginVertical();

            GUILayout.Label("# of FrozenKerbals " + DFFrozenKerbals.Count.ToString(), statusStyle);
            GUILayout.Label("FreezerSize        " + seats, statusStyle);
            GUILayout.Label("FreezerSpace       " + freeseats, statusStyle);
            GUILayout.Label("TotalFrozen        " + totfrozen, statusStyle);
            GUILayout.Label("PartFull           " + partfull, statusStyle);
            GUILayout.Label("IsFreezeActive     " + isfreezeact, statusStyle);
            GUILayout.Label("IsThawActive       " + isthawact, statusStyle);
            GUILayout.Label("FreezerOutofEC     " + outofec, statusStyle);
            GUILayout.Label("crewXferTOActive   " + isxferto, statusStyle);
            GUILayout.Label("crewXferFROMActive " + isxferfrom, statusStyle);
            GUILayout.Label("FrzrTmp            " + tmpsts.ToString(), statusStyle);
            GUILayout.Label("FrozenCrew:");
            if (frzncrewlst.Count == 0)
            {
                GUILayout.Label("NONE");
            }
            else
            {
                foreach (DFWrapper.FrznCrewMbr frzncrewmbr in frzncrewlst)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(frzncrewmbr.CrewName + " " + frzncrewmbr.SeatIdx + frzncrewmbr.VesselName);
                    if (GUILayout.Button("Thaw"))
                    {
                        //this is a nasty example. Assumes only one freezer module and crew is in that first module.
                        //if using this in your mod, you need to cleanly store and look-up the freezer module for the
                        //crewmember and instantiate a DFWrapper.DeepFreezer for that particular module first.
                        DFWrapper.DeepFreezer freezer = new DFWrapper.DeepFreezer(deepfreezers[0]);
                        freezer.beginThawKerbal(frzncrewmbr.CrewName);
                    }
                    GUILayout.EndHorizontal();
                }
            }
            GUILayout.Label("ThawedCrew:");
            if (deepfreezers[0].part.vessel.GetCrewCount() == 0)
            {
                GUILayout.Label("NONE");
            }
            else
            {
                foreach (ProtoCrewMember crew in deepfreezers[0].part.vessel.GetVesselCrew())
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(crew.name);
                    if (GUILayout.Button("Freeze"))
                    {
                        //this is a nasty example. Assumes only one freezer module and crew is in that first module.
                        //if using this in your mod, you need to cleanly store and look-up the freezer module for the
                        //crewmember and instantiate a DFWrapper.DeepFreezer for that particular module first.
                        DFWrapper.DeepFreezer freezer = new DFWrapper.DeepFreezer(deepfreezers[0]);
                        freezer.beginFreezeKerbal(crew);
                    }
                    GUILayout.EndHorizontal();
                }
            }

            GUILayout.EndVertical();
            GUILayout.EndScrollView();
            GUILayout.EndVertical();
            GUI.DragWindow();
        }
    }
}