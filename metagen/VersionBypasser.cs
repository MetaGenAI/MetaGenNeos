/*Neos Version Override from Epsilion :)*/
using BaseX;
using CloudX.Shared;
using FrooxEngine;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
//using FrooxEngine;

namespace metagen
{
    class ASDF
    {
        private static HarmonyLib.Harmony Harmony = new HarmonyLib.Harmony("net.aerizeon.NeosVersionOverride");
        public static void asdf(Engine engine)
        {
            //string compatibilityHash = "z1mXkIlayLh4CfmTTUbRJw==";
            //var t = typeof(Engine);
            //t.GetProperty("CompatibilityHash").SetValue(engine, compatibilityHash, null);
            NeosVersionHelpers.OverrideHash(engine);
            Harmony.PatchAll();
        }
    }
    public static class NeosVersionHelpers
    {
        public static string CurrentVersion { get; set; }
        public static string CurrentHash { get; set; }

        /// <summary>
        /// Searches the MSIL in FrooxEngine.Initialize for the current network version
        /// </summary>
        /// <returns>Neos' current network version</returns>
        public static int GetCurrentVersion()
        {
            try
            {
                /*
                 * Get AsyncStateMachine attribute for FrooxEngine.Initialize, since it is an Async method
                 * This allows us to get the state machine's class from AsyncStateMachineAttribute.StateMachineType
                 */
                var asyncAttribute = typeof(FrooxEngine.Engine).GetMethod("Initialize", BindingFlags.Public | BindingFlags.Instance)?.GetCustomAttribute<AsyncStateMachineAttribute>();
                if (asyncAttribute != null && asyncAttribute.StateMachineType != null)
                {
                    //Get the .MoveNext method from our custom IAsyncStateMachine
                    var asyncTargetMethodInfo = asyncAttribute.StateMachineType.GetMethod("MoveNext", BindingFlags.NonPublic | BindingFlags.Instance);
                    if (asyncTargetMethodInfo != null)
                    {
                        //Read the MSIL from the MoveNext method
                        var ops = HarmonyLib.PatchProcessor.GetOriginalInstructions(asyncTargetMethodInfo);
                        /* 
                         * Store the method's local variable corresponding to a usage of ConcatenatedStream
                         * which occurs near the target value. We can begin searching from here.
                         */
                        var targetLocal = asyncTargetMethodInfo.GetMethodBody().LocalVariables.SingleOrDefault(l => l.LocalType == typeof(ConcatenatedStream));
                        for (int opIndex = 0; opIndex < ops.Count(); opIndex++)
                        {
                            var opcode = ops.ElementAt(opIndex);
                            //Check if this opcode corresponds to the previously defined local
                            if (opcode.IsLdloc(targetLocal as LocalBuilder))
                            {
                                //If so, check the opcode 2 places ahead to see if it's a call to BitConverter.GetBytes(int)
                                opcode = ops.ElementAt(opIndex + 2);
                                if (opcode.Is(OpCodes.Call, typeof(BitConverter).GetMethod("GetBytes", new Type[] { typeof(int) })))
                                {
                                    //If so, check that the opcode 1 place ahead is a constant
                                    opcode = ops.ElementAt(opIndex + 1);
                                    if (opcode != null && opcode.LoadsConstant())
                                    {
                                        //If it is a constant, then it's *probably* the correct constant for our purposes.
                                        if (opcode.operand is int version)
                                        {
                                            //Check if it's an int, and if so, assign it to NetworkVersion for use in OverrideHash below.
                                            return version;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                UniLog.Log("Unable to determine Neos network version: " + ex);
                throw;
            }
            throw new Exception("Unable to determine neos version");
        }
        /// <summary>
        /// Calculates a new CompatabilityHash based on the specified networkVersion and
        /// the list of loaded plugins
        /// </summary>
        /// <param name="engine">FrooxEngine reference</param>
        /// <param name="networkVersion">the current network version from which to base the CompatabilityHash. -1 Means the value is determined automatically</param>
        /// <param name="loadedPlugins">additionally loaded assemblies, to factor into the CompatabilityHash</param>
        public static void OverrideHash(Engine engine, int networkVersion = -1, string versionString = null, Dictionary<string, string> loadedPlugins = null)
        {
            MD5CryptoServiceProvider csp = new MD5CryptoServiceProvider();
            ConcatenatedStream hashStream = new ConcatenatedStream();
            if (networkVersion == -1)
                networkVersion = GetCurrentVersion();
            hashStream.EnqueueStream(new MemoryStream(BitConverter.GetBytes(networkVersion)));
            if (loadedPlugins != null)
            {
                string PluginsBase = PathUtility.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\Plugins\\";
                foreach (string assemblyPath in loadedPlugins.Keys)
                {
                    try
                    {
                        FileStream fileStream = File.OpenRead(PluginsBase + assemblyPath);
                        fileStream.Seek(375L, SeekOrigin.Current);
                        hashStream.EnqueueStream(fileStream);
                    }
                    catch
                    {
                        UniLog.Log("Failed to load assembly for hashing: " + PluginsBase + assemblyPath);
                    }
                }
            }

            AssemblyName currentAssemblyName = Assembly.GetExecutingAssembly().GetName();
            SetHash(Convert.ToBase64String(csp.ComputeHash(hashStream)), versionString ?? (currentAssemblyName.Name + "-" + currentAssemblyName.Version), engine);
        }

        /// <summary>
        /// Overrides the internal definitions of CompatabilityHash and Version String in FrooxEngine
        /// </summary>
        /// <param name="compatHash">the new CompatabilityHash string</param>
        /// <param name="appVersion">the new Version string</param>
        /// <param name="Engine">Reference to FrooxEngine</param>
        public static void SetHash(string compatHash, string appVersion, Engine Engine)
        {
            var versionStringField = typeof(Engine).GetField("_versionString", BindingFlags.NonPublic | BindingFlags.Static);
            var compatHashProperty = typeof(Engine).GetProperty("CompatibilityHash");
            var userStatusField = typeof(StatusManager).GetField("status", BindingFlags.NonPublic | BindingFlags.Instance);
            try
            {
                versionStringField.SetValue(Engine, appVersion);
                compatHashProperty.SetValue(Engine, compatHash, null);
                if (userStatusField.GetValue(Engine.Cloud.Status) is UserStatus status)
                {
                    status.CompatibilityHash = compatHash;
                    status.NeosVersion = appVersion;
                    CurrentVersion = appVersion;
                    CurrentHash = compatHash;
                }
                else
                    UniLog.Error("Failed to override UserStatus CompatibilityHash", false);

            }
            catch (Exception ex)
            {
                UniLog.Error("Failed to override Engine CompatibilityHash: " + ex.ToString(), false);
            }
        }
    }

}
