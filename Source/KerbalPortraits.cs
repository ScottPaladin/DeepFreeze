using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using KSP.UI.Screens.Flight;
using UnityEngine;


namespace DeepFreeze
{
    //This Class does not work very well. It was meant to attach to the Portrait PreFab to build our own list of Portraits.
    //But for some reason it is resulting in duplicated portraits and I have spent hours and hours on this trying to figure it out.
    //So I am doing a brute force approach to get the Portrait GameObjects. I am pretty sure this is breaking the EULA of KSP but pretty sure it is not
    //as I am still ONLY accessing the PUBLIC Portraits Class and fields.
    // I hope Squad open the list up to public as per my request in the next release: http://bugs.kerbalspaceprogram.com/issues/8993

    [KSPAddon(KSPAddon.Startup.Flight, false)]
    class Portraits : MonoBehaviour
    {
        public static readonly List<KerbalPortrait> PortraitList = new List<KerbalPortrait>();

        class PortraitTracker : MonoBehaviour
        {
            private KerbalPortrait _portrait;

            private void Start() // Awake might be too early
            {
                if (transform.GetComponentCached(ref _portrait) == null)
                    Destroy(this);
                else AddPortrait(_portrait);
            }

            private void OnDestroy()
            {
                if (_portrait == null) return; 

                RemovePortrait(_portrait);
            }
        }


        private void Awake()
        {
            //var kpg = KerbalPortraitGallery.Instance;
            
            //AddTracker(kpg.portraitPrefab);

            // uncertain whether KSPAddons created before KerbalPortraits initialized
            // pretty sure they are but too lazy to check
            //kpg.gameObject.GetComponentsInChildren<KerbalPortrait>()
            //    .ToList()
            //    .ForEach(AddTracker);

            //Destroy(gameObject);
        }
        
        // Might only need to edit the prefab once. This will make sure we don't add duplicates
        private static void AddTracker(KerbalPortrait portrait)
        {
            if (portrait.gameObject.GetComponent<PortraitTracker>() != null) return;

            portrait.gameObject.AddComponent<PortraitTracker>();
        }


        private static void AddPortrait(KerbalPortrait portrait)
        {
            if (portrait == null) return;

            PortraitList.AddUnique(portrait);
            //PortraitList.Add(portrait);
        }

        private static void RemovePortrait(KerbalPortrait portrait)
        {
            if (PortraitList.Contains(portrait)) PortraitList.RemoveAll(a => a.crewMember == portrait.crewMember);
        }

        /// <summary>
        /// Destroy Portraits for a kerbal and Unregisters them from the KerbalPortraitGallery
        /// </summary>
        /// <param name="kerbal">the Kerbal we want to delete portraits for</param>
        internal static void DestroyPortrait(Kerbal kerbal)
        {
            //set the kerbal InPart to null - this should stop their portrait from re-spawning.
            kerbal.InPart = null;
            KerbalPortraitGallery.Instance.gameObject.GetComponentsInChildren<KerbalPortrait>().Where(a => a.crewMemberName == kerbal.crewMemberName).ToList().ForEach(x => DestroyObject(x));
            
            KerbalPortraitGallery.Instance.UnregisterActiveCrew(kerbal);
        }

        /// <summary>
        /// Restore the Portrait for a kerbal. If Using Texture Replacer mod you must Re-initialise the Textures after this is called.
        /// Otherwise the kerbal will revert back to stock textures.
        /// The Kerbal's RosterStatus must NOT be DEAD if DeepFreeze Frozen otherwise the Portrait will come up as DEAD.
        /// This method will also call: Kerbal.SetVisibleInPortrait(true) which seems to reset the kerbal's portrait camera update routine.
        /// </summary>
        /// <param name="part">The Part the kerbal is inside</param>
        /// <param name="kerbal">The Kerbal we want to register</param>
        internal static void RestorePortrait(Part part, Kerbal kerbal)
        {
            // Select all KerbalPortrait in our PortraitList where the crewMember name is the kerbal we are interested in.
            // We should NOT get any. If we do, Call DestroyPortrait first then come back.
            //KerbalPortrait portrait = PortraitList.FirstOrDefault(a => a.crewMember == kerbal);
            List<KerbalPortrait> portraits = KerbalPortraitGallery.Instance.gameObject.GetComponentsInChildren<KerbalPortrait>().Where(a => a.crewMemberName == kerbal.crewMemberName).ToList();
            if (portraits.Any())
            {
                //This will destroy ALL Portraits and un-register them as many times as we have an entry for them.
                DestroyPortrait(kerbal);
            }
            //set the kerbal InPart to the Part passed in.
            kerbal.InPart = part;
            //Set them visible in portrait to true
            kerbal.SetVisibleInPortrait(true);
            //This should reset the kerbal portrait back up for us.
            kerbal.state = Kerbal.States.ALIVE;
            kerbal.randomizeOnStartup = false;
            kerbal.Start();   
        }
    }
}
