using LFBetterMusic.Preview;
using View.Evt;

namespace LFBetterMusic.Patches
{
    /// <summary>
    /// 编辑器 PreviewTalkView 原版不会真正执行 cfg.effect，必须由插件桥接。
    /// 两个 Prefix 由 Plugin 手动精确安装并纳入 EFFECT 保活自检。
    /// </summary>
    internal static class PreviewDoTextEndEffectPatch
    {
        internal static void Prefix(PreviewTalkView __instance)
        {
            BetterMusicPreviewBridge.TryTriggerCurrentTalk(__instance);
        }
    }

    internal static class PreviewEmptyTalkEffectPatch
    {
        internal static void Prefix(PreviewTalkView __instance, int __0)
        {
            BetterMusicPreviewBridge.TryTriggerEmptyTalkBeforeRefresh(__instance, __0);
        }
    }
}
