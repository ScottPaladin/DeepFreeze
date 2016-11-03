/**
 * DeepFreeze Continued...
 * (C) Copyright 2015, Jamie Leighton
 *
 * Kerbal Space Program is Copyright (C) 2013 Squad. See http://kerbalspaceprogram.com/. This
 * project is in no way associated with nor endorsed by Squad.
 *
 *  This file is part of JPLRepo's DeepFreeze (continued...) - a Fork of DeepFreeze. Original Author of DeepFreeze is 'scottpaladin' on the KSP Forums.
 *  This File was not part of the original Deepfreeze but was written by Jamie Leighton.
 *  (C) Copyright 2015, Jamie Leighton
 *
 * Continues to be licensed under the Attribution-NonCommercial-ShareAlike 3.0 (CC BY-NC-SA 4.0)
 * creative commons license. See <https://creativecommons.org/licenses/by-nc-sa/4.0/>
 * for full details.
 *
 */
using KSP.UI.Screens.Flight;
using UnityEngine;


namespace DeepFreeze
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    class DFPortraits : MonoBehaviour
    {
        internal static bool HasPortrait(Kerbal crew, bool checkName = false)
        {
            if (!checkName)
            {
                for (int i = 0; i < KerbalPortraitGallery.Instance.Portraits.Count; ++i)
                {
                    if (KerbalPortraitGallery.Instance.Portraits[i].crewMember == crew)
                        return true;
                }
                return false;
            }
            else
            {
                for (int i = 0; i < KerbalPortraitGallery.Instance.Portraits.Count; ++i)
                {
                    if (KerbalPortraitGallery.Instance.Portraits[i].crewMemberName == crew.crewMemberName)
                        return true;
                }
                return false;
            }
        }

        internal static bool InActiveCrew(Kerbal crew, bool checkName = false)
        {
            if (!checkName)
            {
                for (int i = 0; i < KerbalPortraitGallery.Instance.ActiveCrew.Count; ++i)
                {
                    if (KerbalPortraitGallery.Instance.ActiveCrew[i] == crew)
                        return true;
                }
                return false;
            }
            else
            {
                for (int i = 0; i < KerbalPortraitGallery.Instance.ActiveCrew.Count; ++i)
                {
                    if (KerbalPortraitGallery.Instance.ActiveCrew[i].crewMemberName == crew.crewMemberName)
                        return true;
                }
                return false;
            }
        }

        /// <summary>
        /// Destroy Portraits for a kerbal and Unregisters them from the KerbalPortraitGallery
        /// </summary>
        /// <param name="kerbal">the Kerbal we want to delete portraits for</param>
        internal static void DestroyPortrait(Kerbal kerbal)
        {
            // set the kerbal InPart to null - this should stop their portrait from re-spawning.
            kerbal.InPart = null;
            //Set them visible in portrait to false
            kerbal.SetVisibleInPortrait(false);
            kerbal.state = Kerbal.States.NO_SIGNAL;
            //Loop through the ActiveCrew portrait List
            for (int i = KerbalPortraitGallery.Instance.ActiveCrew.Count - 1; i >= 0; i--)
            {
                //If we find an ActiveCrew entry where the crewMemberName is equal to our kerbal's
                if (KerbalPortraitGallery.Instance.ActiveCrew[i].crewMemberName == kerbal.crewMemberName)
                {
                    //we Remove them from the list.
                    KerbalPortraitGallery.Instance.ActiveCrew.RemoveAt(i);
                }
            }
            //Portraits List clean-up.
            KerbalPortraitGallery.Instance.DespawnInactivePortraits(); //Despawn any portraits where CrewMember == null
            KerbalPortraitGallery.Instance.DespawnPortrait(kerbal); //Despawn our Kerbal's portrait
            KerbalPortraitGallery.Instance.UIControlsUpdate(); //Update UI controls
        }

        /// <summary>
        /// Restore the Portrait for a kerbal and register them to the KerbalPortraitGallery
        /// </summary>
        /// <param name="kerbal">the kerbal we want restored</param>
        /// <param name="part">the part the kerbal is in</param>
        internal static void RestorePortrait(Part part, Kerbal kerbal)
        {
            //We don't process DEAD, Unowned kerbals - Compatibility with DeepFreeze Mod.
            if (kerbal.rosterStatus != ProtoCrewMember.RosterStatus.Dead &&
                kerbal.protoCrewMember.type != ProtoCrewMember.KerbalType.Unowned)
            {
                //Set the Kerbals InPart back to their part.
                kerbal.InPart = part;
                //Set their portrait state to ALIVE and set their portrait back to visible.
                kerbal.state = Kerbal.States.ALIVE;
                kerbal.SetVisibleInPortrait(true);
                //Find an ActiveCrew entry and Portraits entry for our kerbal?
                //If they aren't in ActiveCrew and don't have a Portrait them via the kerbal.Start method.
                if (!InActiveCrew(kerbal) && !HasPortrait(kerbal))
                {
                    kerbal.staticOverlayDuration = 1f;
                    kerbal.randomizeOnStartup = false;
                    kerbal.Start();
                }
                kerbal.state = Kerbal.States.ALIVE;
            }
        }
    }
}
