using BepInEx;
using System;
using System.Reflection;
using UnityEngine;

namespace EscMapCloser
{
    internal static class PluginInfo
    {
        internal const string GUID = "mato.escmapcloser";
        internal const string NAME = "EscMapCloser";
        internal const string VERSION = "1.0.0";
    }

    [BepInPlugin(PluginInfo.GUID, PluginInfo.NAME, PluginInfo.VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        private const string TYPE = "Project._Scripts.MiniMap.MiniMap";
        private static readonly string[] MethodCandidates = { "CloseMap", "Close", "ForceClose", "Toggle", "SetOpen" };

        private object target;
        private MethodInfo method;

        void Update()
        {
            if (!Input.GetKeyDown(KeyCode.Escape)) return;
            if (method == null && !PrepareHook()) return;

            try
            {
                var ps = method.GetParameters();
                if (ps.Length == 0) method.Invoke(target, null);
                else if (ps.Length == 1 && ps[0].ParameterType == typeof(bool))
                    method.Invoke(target, new object[] { false });
                Input.ResetInputAxes();
            }
            catch (Exception e)
            {
                Logger.LogWarning("[EscMapCloser] Invoke failed: " + e.Message);
                method = null; target = null;
            }
        }

        private bool PrepareHook()
        {
            var t = FindType(TYPE);
            if (t == null) return false;
            target = UnityEngine.Object.FindObjectOfType(t);
            if (target == null)
            {
                var prop = t.GetProperty("Instance", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                if (prop != null) target = prop.GetValue(null, null);
            }
            foreach (var name in MethodCandidates)
            {
                var m = t.GetMethod(name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                if (m == null) continue;
                var ps = m.GetParameters();
                if (ps.Length == 0 || (ps.Length == 1 && ps[0].ParameterType == typeof(bool)))
                {
                    method = m;
                    Logger.LogInfo($"[EscMapCloser] Hooked {t.FullName}.{m.Name}");
                    return true;
                }
            }
            return false;
        }

        private static Type FindType(string fullName)
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                try { var t = asm.GetType(fullName, false); if (t != null) return t; }
                catch { }
            }
            return null;
        }
    }
}
