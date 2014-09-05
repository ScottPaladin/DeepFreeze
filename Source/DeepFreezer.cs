using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using DeepFreezer;


namespace DeepFreezer
{
    [KSPAddon(KSPAddon.Startup.EveryScene, false)]
    class DeepFreeze : MonoBehaviour
    {

        public void Start()
        {
            if (!DeepFreezeEvents.instance.eventAdded)
                Debug.Log(DeepFreezeEvents.instance.eventAdded);
            {
                DeepFreezeEvents.instance.DeepFreezeEventAdd();
                Debug.Log("Start called");
            }
        }


    }
    public class DeepFreezer : PartModule
    {
        private float lastUpdate = 0.0f;
        private float lastFixedUpdate = 0.0f;
        private float updatetnterval = .5f;

        [KSPField(isPersistant = true, guiActive = true, guiName = "Frozen Crew")]
        public string FrozenCrew;

        [KSPField(isPersistant = true, guiActive = false, guiName = "Freezer Size")]
        public int FreezerSize;

        [KSPField(isPersistant = true, guiActive = true, guiName = "Total Frozen Kerbals")]
        public int TotalFrozen;

        [KSPField(isPersistant = true, guiActive = true, guiName = "Freezer Space")]
        public int FreezerSpace;

        [KSPField(isPersistant = true)]
        public bool IsCrewableWhenFull;

        public List<string> StoredCrew;

        public override void OnUpdate()
        {
            if ((Time.time - lastUpdate) > updatetnterval)
            {
                base.OnInactive();
                lastUpdate = Time.time;
                UpdateEvents();
                FrozenCrew = String.Join(",", StoredCrew.ToArray());
            }
        }

        public override void OnLoad(ConfigNode node)
        {
            FrozenCrew = node.GetValue("FrozenCrew");
            //Debug.Log(FrozenCrew);
            Int32.TryParse(node.GetValue("FreezerSize"), out FreezerSize);
            //Debug.Log(FreezerSize + " " + FreezerSpace);
            IsCrewableWhenFull = Convert.ToBoolean(node.GetValue("IsCrewableWhenFull"));
            LoadFrozenCrew();
        }
        public override void OnSave(ConfigNode node)
        {
            node.SetValue("FrozenCrew", FrozenCrew);
        }
        public override void OnInactive()
        {
            Debug.Log("OnInactive" + FrozenCrew);
            part.CrewCapacity = StoredCrew.Count;
            foreach (var crewmember in StoredCrew)
            {

                foreach (ProtoCrewMember kerbal in HighLogic.CurrentGame.CrewRoster.Crew)
                {
                    if (kerbal.name == crewmember)
                        part.AddCrewmember(kerbal);
                }

            }
            base.OnInactive();
        }

        private void LoadFrozenCrew() //The FrozenCrew variable is a string, we need it split into a list.
        {
            if (!string.IsNullOrEmpty(FrozenCrew))
            {
                StoredCrew = FrozenCrew.Split(',').ToList();
            }
        }

        private void UpdateEvents()
        {
            UpdateCounts();
            Events.Clear();
            if (StoredCrew.Count < FreezerSize)
            {
                part.CrewCapacity = 1;
                foreach (var CrewMember in part.protoModuleCrew)
                {
                    Events.Add(new BaseEvent(Events, "Freeze " + CrewMember.name, () =>
                    {
                        if ((FreezerSize - StoredCrew.Count) <= 0)
                        {
                            ScreenMessages.PostScreenMessage("No Space Left in the Freezer. Aborting Freezing.", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                        }
                        if ((FreezerSize - StoredCrew.Count) > 0)
                        {
                            if (!requireResource(vessel, "Glykerol", 10, true))
                            {
                                ScreenMessages.PostScreenMessage("Insufficient Glykerol to freeze kerbal.", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                                return;
                            }
                            FreezeKerbal(CrewMember);

                        }

                    }, new KSPEvent { guiName = "Freeze " + CrewMember.name, guiActive = true }));
                }
            }
            if ((part.protoModuleCrew.Count < part.CrewCapacity) || part.CrewCapacity <= 0)
                foreach (var frozenkerbal in StoredCrew)
                {
                    Events.Add(new BaseEvent(Events, "Thaw" + frozenkerbal, () =>
                    {
                        ThawKerbal(frozenkerbal);

                    }, new KSPEvent { guiName = "Thaw " + frozenkerbal, guiActive = true }));
                }
            if (StoredCrew.Count >= FreezerSize && IsCrewableWhenFull == true)
            {
                part.CrewCapacity = 0;
            }

        }
        public void FreezeKerbal(ProtoCrewMember CrewMember)
        {
            part.RemoveCrewmember(CrewMember);
            StoredCrew.Add(CrewMember.name);
            CrewMember.rosterStatus = ProtoCrewMember.RosterStatus.Dead;
            ScreenMessages.PostScreenMessage(CrewMember.name + " frozen", 5.0f, ScreenMessageStyle.UPPER_CENTER);
            FrozenCrew = String.Join(",", StoredCrew.ToArray());
            //Debug.Log("FrozenCrew =" + FrozenCrew);
            UpdateCounts();
        }
        public void ThawKerbal(String frozenkerbal)
        {
            foreach (ProtoCrewMember kerbal in HighLogic.CurrentGame.CrewRoster.Crew) //There's probably a more efficient way to find Protocrewmember from the CrewRoster
            {
                if (kerbal.name == frozenkerbal)
                {
                    part.CrewCapacity = 1;
                    part.AddCrewmember(kerbal);
                    ScreenMessages.PostScreenMessage(kerbal.name + " thawed out.", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                    //Debug.Log(StoredCrew.Remove(kerbal.name));
                    FrozenCrew = String.Join(",", StoredCrew.ToArray());
                    //Debug.Log("FrozenCrew =" + FrozenCrew);
                    UpdateCounts();

                }
            }
        }
        // Simple bool for resource checking and usage.  Returns true and optionally uses resource if resAmount of res is available. - Credit TMarkos https://github.com/TMarkos/ as this is lifted verbatim from his Beacon's pack. Mad modify as needed.
        public bool requireResource(Vessel craft, string res, double resAmount, bool consumeResource)
        {
            if (!craft.loaded) return false; // Unloaded resource checking is unreliable.
            Dictionary<PartResource, double> toDraw = new Dictionary<PartResource, double>();
            double resRemaining = resAmount;
            foreach (Part cPart in craft.Parts)
            {
                foreach (PartResource cRes in cPart.Resources)
                {
                    if (cRes.resourceName != res) continue;
                    if (cRes.amount == 0) continue;
                    if (cRes.amount >= resRemaining)
                    {
                        toDraw.Add(cRes, resRemaining);
                        resRemaining = 0;
                    }
                    else
                    {
                        toDraw.Add(cRes, cRes.amount);
                        resRemaining -= cRes.amount;
                    }
                }
                if (resRemaining <= 0) break;
            }
            if (resRemaining > 0) return false;
            if (consumeResource)
            {
                foreach (KeyValuePair<PartResource, double> drawSource in toDraw)
                {
                    drawSource.Key.amount -= drawSource.Value;
                }
            }
            return true;
        }
        public void UpdateCounts()
        {
            FreezerSpace = (FreezerSize - StoredCrew.Count);
            TotalFrozen = StoredCrew.Count;
        }
    }

}
