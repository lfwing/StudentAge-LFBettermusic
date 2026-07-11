using Config;
using HarmonyLib;
using View.Evt;

namespace LFBetterMusic.Runtime
{
    internal static class RuntimeTalkAccess
    {
        private static readonly System.Reflection.FieldInfo CfgField = AccessTools.Field(typeof(NewTalkView), "cfg");

        public static TalkCfg GetCurrentCfg(NewTalkView view)
        {
            return view == null ? null : CfgField?.GetValue(view) as TalkCfg;
        }

        public static int TryGetTalkId(NewTalkView view)
        {
            return GetCurrentCfg(view)?.id ?? 0;
        }
    }
}
