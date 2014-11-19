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
            //Debug.Log("Start called");
            if (!DeepFreezeEvents.instance.eventAdded)
            //{
                DeepFreezeEvents.instance.DeepFreezeEventAdd();
                Debug.Log("!DeepFreezeEvents.instance.eventAdded");
           // }
        }


    }
    public class DeepFreezer : PartModule
    {
        private float lastUpdate = 0.0f;

        private float updatetnterval = .5f;


        [KSPField(isPersistant = true, guiActive = false, guiName = "FC")] //This string value is the names of frozen crew, it is turned into a list called StoredCrew during loaded. We keep this string current and it get's saved to the persistant.sfs on save.
        public string FrozenCrew;

        [KSPField(isPersistant = true, guiActive = false, guiName = "Freezer Size")] //Total Size of Freezer, get's read from part.cfg.
        public int FreezerSize;

        [KSPField(isPersistant = true, guiActive = true, guiName = "Total Frozen Kerbals")] //WISOTT Total number of frozen kerbals, just a count of the list object.
        public int TotalFrozen;

        [KSPField(isPersistant = true, guiActive = true, guiName = "Freezer Space")] //Total space available for storage. Set by Part.cfg file.
        public int FreezerSpace;

        [KSPField(isPersistant = true)]
        public bool IsCrewableWhenFull; //Set by part.cfg. Intended to set whether a kerbal can enter the part when the frozen storage is filled up. Especially important for the single kerbal part.

        [KSPField()]
        public bool IsFreezeActive;

        [KSPField()]
        public bool IsThawActive;

        [KSPField()]
        public double StoredCharge;

        [KSPField(isPersistant = true, guiActive = true, guiName = "ChargeRequired")]
        public Int32 ChargeRequired; //Set by part.cfg. Total EC value required for a complete freeze or thaw.

        [KSPField(isPersistant = true, guiActive = true, guiName = "ChargeRate")]
        public Int32 ChargeRate; //Set by part.cfg. EC draw per tick.


        public ProtoCrewMember ActiveKerbal;
        public string ToThawKerbal;

        public List<string> StoredCrew;
        
        protected AudioSource hatch_lock;
        protected AudioSource ice_freeze;
        protected AudioSource machine_hum;
        protected AudioSource ding_ding;

        public override void OnUpdate()
        {
            if ((Time.time - lastUpdate) > updatetnterval)
            {

                lastUpdate = Time.time;
                UpdateEvents();
                //StoredCrew = StoredCrew.Distinct().ToList();
                if (StoredCrew.Count > 0)
                {
                    FrozenCrew = String.Join(",", StoredCrew.ToArray());
                }
            }
        }

        public void FixedUpdate()
        {

            if (IsFreezeActive == true)
            {

                if (!requireResource(vessel, "ElectricCharge", ChargeRate, false) == true)
                {

                    ScreenMessages.PostScreenMessage("Insufficient electric charge to freeze kerbal.", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                    FreezeKerbalAbort(ActiveKerbal);
                    return;
                }
                else
                {
                    requireResource(vessel, "ElectricCharge", ChargeRate, true);
                    StoredCharge = StoredCharge + ChargeRate;
                    //Debug.Log("Drawing Charge");
                    if (StoredCharge > ChargeRequired)
                    {
                        if (requireResource(vessel, "Glykerol", 5, true))
                        {
                            FreezeKerbalConfirm(ActiveKerbal);
                        }
                        else
                        {
                           
                            FreezeKerbalAbort(ActiveKerbal);
                        }
                    }
                }

            }
            if (IsThawActive == true)
            {
                if (!requireResource(vessel, "ElectricCharge", ChargeRate, false))
                {
                    ScreenMessages.PostScreenMessage("Insufficient electric charge to thaw kerbal.", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                    ThawKerbalAbort(ToThawKerbal);
                }
                else
                {
                    requireResource(vessel, "ElectricCharge", ChargeRate, true);
                    StoredCharge = StoredCharge + ChargeRate;
                    if (StoredCharge > ChargeRequired)
                    {
                        ThawKerbalConfirm(ToThawKerbal);
                    }
                }
            }
        }


        public override void OnLoad(ConfigNode node)
        {
            //ChargeRequired = 3000;
            //ChargeRate = 20;
            Int32.TryParse(node.GetValue("ChargeRequired"), out ChargeRequired);
            Int32.TryParse(node.GetValue("ChargeRate"), out ChargeRate);
            IsCrewableWhenFull = Convert.ToBoolean(node.GetValue("IsCrewableWhenFull"));
            FrozenCrew = node.GetValue("FrozenCrew");
            //Debug.Log(FrozenCrew);
            LoadFrozenCrew();
        }

        public override void OnStart(PartModule.StartState state)
        {
            hatch_lock = gameObject.AddComponent<AudioSource>();
            hatch_lock.clip = GameDatabase.Instance.GetAudioClip("PaladinLabs/DeepFreeze/Sounds/hatch_lock");
            hatch_lock.volume = .5F;
            hatch_lock.panLevel = 0;
            hatch_lock.rolloffMode = AudioRolloffMode.Linear;
            hatch_lock.Stop();
            ice_freeze = gameObject.AddComponent<AudioSource>();
            ice_freeze.clip = GameDatabase.Instance.GetAudioClip("PaladinLabs/DeepFreeze/Sounds/ice_freeze");
            ice_freeze.volume = 1;
            ice_freeze.panLevel = 0;
            ice_freeze.rolloffMode = AudioRolloffMode.Linear;
            ice_freeze.Stop();
            machine_hum = gameObject.AddComponent<AudioSource>();
            machine_hum.clip = GameDatabase.Instance.GetAudioClip("PaladinLabs/DeepFreeze/Sounds/machine_hum");
            machine_hum.volume = .1F;
            machine_hum.panLevel = 0;
            machine_hum.rolloffMode = AudioRolloffMode.Linear;
            machine_hum.Stop();
            ding_ding = gameObject.AddComponent<AudioSource>();
            ding_ding.clip = GameDatabase.Instance.GetAudioClip("PaladinLabs/DeepFreeze/Sounds/ding_ding");
            ding_ding.volume = .25F;
            ding_ding.panLevel = 0;
            ding_ding.rolloffMode = AudioRolloffMode.Linear;
            ding_ding.Stop();
        }

        public override void OnSave(ConfigNode node)
        {
            node.SetValue("FrozenCrew", FrozenCrew);
            //Debug.Log("OnSave: " + node);
        }
        public override void OnInactive()
        {
            //Debug.Log("OnInactive" + FrozenCrew);
            part.CrewCapacity = StoredCrew.Count;
            foreach (var crewmember in StoredCrew)
            {

                foreach (ProtoCrewMember kerbal in HighLogic.CurrentGame.CrewRoster.Crew)
                {
                    if (kerbal.name == crewmember)
                        part.AddCrewmember(kerbal);
                }

            }
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
            if (!IsThawActive && !IsFreezeActive)
            {
                if (StoredCrew.Count < FreezerSize)
                {
                    part.CrewCapacity = 1;
                    foreach (var CrewMember in part.protoModuleCrew)
                    {
                        Events.Add(new BaseEvent(Events, "Freeze " + CrewMember.name, () =>
                        {
                            if ((FreezerSize - StoredCrew.Count) > 0 && part.protoModuleCrew.Contains(CrewMember))
                            {
                                if (!requireResource(vessel, "Glykerol", 5, false))
                                {
                                    ScreenMessages.PostScreenMessage("Insufficient Glykerol to freeze kerbal.", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                                    return;
                                }
                                else
                                {
                                    FreezeKerbal(CrewMember);
                                    //part.Events["Freeze " + CrewMember.name].guiActive = false;
                                }
                            }

                        }, new KSPEvent { guiName = "Freeze " + CrewMember.name, guiActive = true }));
                    }
                }
                if ((part.protoModuleCrew.Count < part.CrewCapacity) || part.CrewCapacity <= 0)
                    foreach (var frozenkerbal in StoredCrew)
                    {
                        Events.Add(new BaseEvent(Events, "Thaw" + frozenkerbal, () =>
                        {
                            if (StoredCrew.Contains(frozenkerbal))
                            {
                                StoredCrew.Remove(frozenkerbal);
                                ToThawKerbal = frozenkerbal;
                                IsThawActive = true;
                                hatch_lock.Play();
                                machine_hum.Play();
                                machine_hum.loop = true;
                            }

                        }, new KSPEvent { guiName = "Thaw " + frozenkerbal, guiActive = true }));
                    }
            }
            if (StoredCrew.Count >= FreezerSize && IsCrewableWhenFull == true)
            {
                part.CrewCapacity = 0;
            }

        }
        public void FreezeKerbal(ProtoCrewMember CrewMember)
        {

            //Debug.Log("Freeze kerbal called");
            part.CrewCapacity = 0;
            part.RemoveCrewmember(CrewMember);
            ActiveKerbal = CrewMember;
            IsFreezeActive = true;
            ScreenMessages.PostScreenMessage("Starting Freeze", 5.0f, ScreenMessageStyle.UPPER_CENTER);
            //Debug.Log("FrozenCrew =" + FrozenCrew);
            UpdateCounts();
            Events.Clear();
            hatch_lock.Play();
            machine_hum.Play();
            machine_hum.loop = true;
        }

        public void FreezeKerbalAbort(ProtoCrewMember kerbal)
        {

            ScreenMessages.PostScreenMessage("Freezing Aborted", 5.0f, ScreenMessageStyle.UPPER_CENTER);
            part.CrewCapacity = 1;
            part.AddCrewmember(kerbal);
            IsFreezeActive = false;
            ActiveKerbal = null;
            machine_hum.Stop();

        }
        public void ThawKerbalAbort(String ThawKerbal)
        {
            ScreenMessages.PostScreenMessage("Thawing Aborted", 5.0f, ScreenMessageStyle.UPPER_CENTER);
            IsThawActive = false;
            StoredCrew.Add(ThawKerbal);
            StoredCharge = 0;
            machine_hum.Stop();
        }
        public void FreezeKerbalConfirm(ProtoCrewMember kerbal)
        {
            machine_hum.Stop();
            StoredCharge = 0;
            StoredCrew.Add(kerbal.name);
            StoredCrew = StoredCrew.Distinct().ToList();
            kerbal.rosterStatus = ProtoCrewMember.RosterStatus.Dead;
            UpdateCounts();
            IsFreezeActive = false;
            ActiveKerbal = null;
            ScreenMessages.PostScreenMessage(kerbal.name + " frozen", 5.0f, ScreenMessageStyle.UPPER_CENTER);
            ice_freeze.Play();

        }
        public void ThawKerbalConfirm(String frozenkerbal)
        {
            foreach (ProtoCrewMember kerbal in HighLogic.CurrentGame.CrewRoster.Crew) //There's probably a more efficient way to find Protocrewmember from the CrewRoster
            {
                if (kerbal.name == frozenkerbal)
                {
                    machine_hum.Stop();
                    StoredCharge = 0;
                    part.CrewCapacity = 1;
                    part.AddCrewmember(kerbal);
                    ScreenMessages.PostScreenMessage(kerbal.name + " thawed out.", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                    //Debug.Log(StoredCrew.Remove(kerbal.name));
                    ToThawKerbal = null;
                    IsThawActive = false;

                    FrozenCrew = String.Join(",", StoredCrew.ToArray());
                    //Debug.Log("FrozenCrew =" + FrozenCrew);
                    UpdateCounts();
                    Events.Clear();
                    ding_ding.Play();

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
