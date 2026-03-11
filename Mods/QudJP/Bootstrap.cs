using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using XRL;

namespace QudJP
{
    [HasModSensitiveStaticCache]
    public static class QudJPLoader
    {
        [ModSensitiveCacheInit]
        public static void Bootstrap()
        {
            try
            {
                Trace.TraceInformation("[QudJP] Bootstrap: resolving QudJP.dll path...");

                string modPath = null;
                foreach (var mod in ModManager.Mods)
                {
                    if (mod.ID == "QudJP")
                    {
                        modPath = mod.Path;
                        break;
                    }
                }

                if (modPath == null)
                {
                    Trace.TraceError("[QudJP] Bootstrap: mod 'QudJP' not found in ModManager.Mods");
                    throw new InvalidOperationException("[QudJP] Bootstrap: mod 'QudJP' not found in ModManager.Mods");
                }

                string dllPath = System.IO.Path.Combine(modPath, "Assemblies", "QudJP.dll");

                if (!File.Exists(dllPath))
                {
                    Trace.TraceError("[QudJP] Bootstrap: QudJP.dll not found at " + dllPath);
                    throw new FileNotFoundException("[QudJP] Bootstrap: QudJP.dll not found at " + dllPath, dllPath);
                }

                Assembly assembly = null;
                foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (asm.GetName().Name == "QudJP")
                    {
                        assembly = asm;
                        break;
                    }
                }

                if (assembly == null)
                {
                    Trace.TraceInformation("[QudJP] Bootstrap: loading assembly from " + dllPath);
                    assembly = Assembly.LoadFrom(dllPath);
                }
                else
                {
                    Trace.TraceInformation("[QudJP] Bootstrap: assembly already loaded.");
                }

                Type modType = assembly.GetType("QudJP.QudJPMod");
                if (modType == null)
                {
                    Trace.TraceError("[QudJP] Bootstrap: type 'QudJP.QudJPMod' not found in assembly");
                    throw new InvalidOperationException("[QudJP] Bootstrap: type 'QudJP.QudJPMod' not found in assembly");
                }

                MethodInfo initMethod = modType.GetMethod("Init", BindingFlags.Public | BindingFlags.Static);
                if (initMethod == null)
                {
                    Trace.TraceError("[QudJP] Bootstrap: method 'Init' not found on QudJP.QudJPMod");
                    throw new InvalidOperationException("[QudJP] Bootstrap: method 'Init' not found on QudJP.QudJPMod");
                }

                initMethod.Invoke(null, null);

                Trace.TraceInformation("[QudJP] Bootstrap: initialization complete.");
            }
            catch (Exception ex)
            {
                Trace.TraceError("[QudJP] Bootstrap failed: " + ex);
                throw;
            }
        }
    }
}
