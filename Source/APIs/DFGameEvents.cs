using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DF
{
    class DFGameEvents
    {
        /// <summary>
        /// Fires when DeepFreeze Completes the Freezing process on a Kerbal.
        /// Part is the DeepFreeze Freezer Part and ProtoCrewMember is the Kerbal.
        /// </summary>
        public static EventData<Part, ProtoCrewMember> onKerbalFrozen = new EventData<Part, ProtoCrewMember>("onKerbalFrozen");
        /// <summary>
        /// Fires when DeepFreeze Completes the Thawing process on a Kerbal.
        /// Part is the DeepFreeze Freezer Part and ProtoCrewMember is the Kerbal.
        /// </summary>
        public static EventData<Part, ProtoCrewMember> onKerbalThaw = new EventData<Part, ProtoCrewMember>("onKerbalThaw");
        /// <summary>
        /// Fires when DeepFreeze sets a Kerbal to Comatose Status.
        /// Part is the DeepFreeze Freezer Part and ProtoCrewMember is the Kerbal.
        /// </summary>
        public static EventData<Part, ProtoCrewMember> onKerbalSetComatose = new EventData<Part, ProtoCrewMember>("onKerbalSetComatose");
        /// <summary>
        /// Fires when DeepFreeze Unsets a Kerbal from Comatose Status.
        /// Part is the DeepFreeze Freezer Part and ProtoCrewMember is the Kerbal.
        /// </summary>
        public static EventData<Part, ProtoCrewMember> onKerbalUnSetComatose = new EventData<Part, ProtoCrewMember>("onKerbalUnSetComatose");
        /// <summary>
        /// Fires when DeepFreeze has to Kill a Frozen Kerbal.
        /// </summary>
        public static EventData<ProtoCrewMember> onFrozenKerbalDied = new EventData<ProtoCrewMember>("onFrozenKerbalDied");

    }
}
