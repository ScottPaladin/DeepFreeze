using System;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;

namespace DF
{
    public interface IDFInterface
    {
        Dictionary<string, KerbalInfo> FrozenKerbals { get; }
    }

    public static class DFInterface
    {
        private static bool _dfChecked = false;
        private static bool _dfInstalled = false;
        public static bool IsDFInstalled
        {
            get
            {
                if (!_dfChecked)
                {
                    string assemblyName = "DeepFreeze";
                    var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                    var assembly = (from a in assemblies
                                    where a.FullName.Contains(assemblyName)
                                    select a).SingleOrDefault();
                    if (assembly != null)
                        _dfInstalled = true;
                    else
                        _dfInstalled = false;
                    _dfChecked = true;
                }
                return _dfInstalled;
            }
        }

        public static IDFInterface GetFrozenKerbals()
        {
            IDFInterface _IDFobj = null;
            Type SMAddonType = AssemblyLoader.loadedAssemblies.SelectMany(a => a.assembly.GetExportedTypes()).SingleOrDefault(t => t.FullName == "ShipManifest.CrewTransfer");
            if (SMAddonType != null)
            {
                object crewTransferObj = SMAddonType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static).GetValue(null, null);
                _IDFobj = (IDFInterface)crewTransferObj;
            }
            return _IDFobj;
        }
    }
}
