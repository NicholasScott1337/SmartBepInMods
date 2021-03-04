using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using SmartBepInMods.Tools;

namespace SmartBepInMods.Tools.Patching
{
    /// <summary>
    /// Patcher service for patching methods in segments.
    /// </summary>
    public static class SmartPatcher
    {
        private static Harmony _instance;
        /// <summary>
        /// The instance of Harmony for this plugin.
        /// </summary>
        public static Harmony Instance { get { _instance = _instance ?? new Harmony("com.nicholascott.autopatcher"); return _instance; } }

        /// <summary>
        /// Patch all methods related to the current environment(Server or Client)
        /// Defaults to Shared!
        /// </summary>
        /// <param name="assembly">Your Assembly.GetExecutingAssembly().</param>
        /// <param name="log">Logging method.</param>
        /// <param name="debug">Optionally trigger DEBUG instead</param>
        public static void PatchGameAuto(this Assembly assembly, Log log, bool debug = false)
        {
            if (debug)
            {
                assembly.PatchGameRaw<Constants.DEBUG>(log);
                return;
            }
            PatchGameAuto(assembly, log);
        }
        /// <summary>
        /// Patch all methods within the type derived from Constant.
        /// </summary>
        /// <typeparam name="T">Type to search for.</typeparam>
        /// <param name="assembly">Your Assembly.GetExecutingAssembly()</param>
        /// <param name="log">Logging method.</param>
        public static void PatchGameRaw<T>(this Assembly assembly, Log log)
        {
            log($"[SmartPatcher] Running {typeof(T).Name} version via manual trigger.");
            PatchGameRaw(typeof(T), assembly, log);
        }
        private static void PatchGameAuto(Assembly assembly, Log log)
        {
            var baseClass = Constants.CONST.DeriveEnvironmentArgs(assembly, log);
            log($"[SmartPatcher] Running {baseClass.Final.Name} version via auto-detection.");

            PatchGameRaw(baseClass.Final, assembly, log);
        }
        private static void PatchGameRaw(Type baseClass, Assembly assembly, Log log)
        {
            var x = assembly.GetTypes()
                .Where(type => baseClass.IsAssignableFrom(type) && baseClass != type);
            foreach (var y in x)
            {
                log($"[{baseClass.Name}] Patching {y.Name}");
                var lol = Instance.ProcessorForAnnotatedClass(y);
                lol.Patch();
            }

            log($"[{baseClass.Name}] Patched {Instance.GetPatchedMethods().ToArray().Length} method{(Instance.GetPatchedMethods().ToArray().Length != 1 ? "s" : "")}.");
        }
    }
    namespace Constants
    {
        /// <summary>
        /// Holding class for stuff :D
        /// </summary>
        public static class CONST
        {
            internal struct EnvData
            {
                public IEnumerable<string> args;
                public Type Final;
            }
            internal static EnvData DeriveEnvironmentArgs(Assembly assembly, Log log)
            {
                EnvData ret = new EnvData();
                ret.Final = typeof(SHARED);

                var x = assembly.Location.Split('\\');
                var fileName = x[x.Length - 1];
                var y = fileName.Split('.').ToList();
                if (y.Count < 3) return ret;

                y.RemoveAt(y.Count - 1);
                y.RemoveAt(0);

                ret.args = y;

                var z = Assembly.GetExecutingAssembly().GetTypes()
                    .Where(type => typeof(Patchable).IsAssignableFrom(type));
                var z3 = assembly.GetTypes()
                    .Where(type => typeof(Patchable).IsAssignableFrom(type));
                foreach (var z2 in z)
                {
                    var name = z2.Name.ToLower();
                    if (y[0].ToLower() == name)
                    {
                        ret.Final = z2;
                        return ret;
                    }
                }
                foreach (var z2 in z3)
                {
                    var name = z2.Name.ToLower();
                    if (y[0].ToLower() == name)
                    {
                        ret.Final = z2;
                        return ret;
                    }
                }
                return ret;
            }
            /// <summary>
            /// Gets the Patchable type that represents the current state.
            /// </summary>
            /// <param name="assembly">This</param>
            /// <param name="log">Logging object.</param>
            /// <returns></returns>
            public static Type GetEnvArg(this Assembly assembly, Log log)
            {
                return DeriveEnvironmentArgs(assembly, log).Final;
            }
        }
        /// <summary>
        /// This is the root of all "Labels".
        /// Deriving a class from this will make a new label.
        /// You can then do <b>Patchable.Patch()</b>.
        /// </summary>
        public abstract class Patchable
        {
        }
        /// <summary>
        /// CLIENT Label
        /// </summary>
        public abstract class CLIENT : Patchable { }
        /// <summary>
        /// SERVER Label
        /// </summary>
        public abstract class SERVER : Patchable { }
        /// <summary>
        /// SHARED Label
        /// </summary>
        public abstract class SHARED : Patchable { }
        /// <summary>
        /// DEBUG Label
        /// </summary>
        public abstract class DEBUG : Patchable { }
    }
}