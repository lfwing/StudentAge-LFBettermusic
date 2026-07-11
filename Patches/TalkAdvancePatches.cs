using System;
using HarmonyLib;
using LFBetterMusic.Runtime;
using Sdk;
using View.Evt;

namespace LFBetterMusic.Patches
{
    /// <summary>
    /// 原版自动模式会把 OnClickNext 作为延时回调交给 TimerMgr。
    /// 包装该回调，确保“自动推进”不会被误判为玩家手动点击。
    /// </summary>
    [HarmonyPatch(
        typeof(TimerMgr),
        nameof(TimerMgr.Delay),
        new Type[] { typeof(Action), typeof(float), typeof(int) })]
    internal static class AutoTalkDelayContextPatch
    {
        private static void Prefix(ref Action __0)
        {
            Action original = __0;
            if (original == null || !(original.Target is NewTalkView owner) ||
                original.Method.Name != nameof(NewTalkView.OnClickNext))
            {
                return;
            }

            __0 = () =>
            {
                BetterMusicController controller = BetterMusicController.Instance;
                controller?.BeginAdvance(owner, TalkAdvanceOrigin.Automatic);
                try
                {
                    original();
                }
                finally
                {
                    controller?.EndAdvance(owner);
                }
            };
        }
    }

    /// <summary>
    /// 开启自动模式时，原版可能立即调用一次 OnClickNext。
    /// </summary>
    [HarmonyPatch(typeof(NewTalkView), nameof(NewTalkView.AutoTalk))]
    internal static class NewTalkAutoTalkContextPatch
    {
        private static void Prefix(NewTalkView __instance, bool __0, out bool __state)
        {
            __state = __0;
            if (__state)
            {
                BetterMusicController.Instance?.BeginAdvance(
                    __instance,
                    TalkAdvanceOrigin.Automatic);
            }
        }

        private static void Postfix(NewTalkView __instance, bool __state)
        {
            if (__state)
            {
                BetterMusicController.Instance?.EndAdvance(__instance);
            }
        }
    }

    [HarmonyPatch(typeof(NewTalkView), nameof(NewTalkView.OnClickNext))]
    internal static class NewTalkClickContextPatch
    {
        private static void Prefix(NewTalkView __instance, out bool __state)
        {
            BetterMusicController controller = BetterMusicController.Instance;
            __state = controller != null;
            controller?.BeginAdvance(__instance, TalkAdvanceOrigin.Manual);
        }

        private static void Postfix(NewTalkView __instance, bool __state)
        {
            if (__state)
            {
                BetterMusicController.Instance?.EndAdvance(__instance);
            }
        }
    }

    [HarmonyPatch(typeof(PreviewTalkView), nameof(PreviewTalkView.OnClickNext))]
    internal static class PreviewTalkClickContextPatch
    {
        private static void Prefix(PreviewTalkView __instance, out bool __state)
        {
            BetterMusicController controller = BetterMusicController.Instance;
            __state = controller != null;
            controller?.BeginAdvance(__instance, TalkAdvanceOrigin.Manual);
        }

        private static void Postfix(PreviewTalkView __instance, bool __state)
        {
            if (__state)
            {
                BetterMusicController.Instance?.EndAdvance(__instance);
            }
        }
    }

    [HarmonyPatch(typeof(NewTalkView), nameof(NewTalkView.NextTalk))]
    internal static class NewTalkNextPatch
    {
        private static bool Prefix(NewTalkView __instance)
        {
            BetterMusicController controller = BetterMusicController.Instance;
            return controller == null || controller.HandleNextTalkAttempt(__instance);
        }
    }

    [HarmonyPatch(typeof(PreviewTalkView), nameof(PreviewTalkView.NextTalk))]
    internal static class PreviewTalkNextPatch
    {
        private static bool Prefix(PreviewTalkView __instance)
        {
            BetterMusicController controller = BetterMusicController.Instance;
            return controller == null || controller.HandleNextTalkAttempt(__instance);
        }
    }
}
