using System;
using HarmonyLib;
using LFBetterMusic.Preview;
using LFBetterMusic.Runtime;
using Sdk;
using View.Event;
using View.Evt;

namespace LFBetterMusic.Patches
{
    [HarmonyPatch(typeof(NewTalkView), nameof(NewTalkView.RefreshTalk))]
    internal static class NewTalkRefreshPatch
    {
        private static void Prefix(NewTalkView __instance, int __0)
        {
            BetterMusicController.Instance?.BeforeTalkRefresh(__instance, __0);
        }

        private static void Postfix(NewTalkView __instance)
        {
            int currentTalkId = RuntimeTalkAccess.TryGetTalkId(__instance);
            BetterMusicController.Instance?.AfterTalkRefresh(__instance, currentTalkId);
        }
    }


    [HarmonyPatch(typeof(CommonTalkView), nameof(CommonTalkView.RefreshTalk))]
    internal static class CommonTalkRefreshLifecyclePatch
    {
        private static void Prefix(CommonTalkView __instance, int __0)
        {
            BetterMusicController.Instance?.BeforeTalkRefresh(__instance, __0);
        }

        private static void Postfix(CommonTalkView __instance)
        {
            BetterMusicController.Instance?.AfterTalkRefresh(__instance, -1);
        }
    }

    [HarmonyPatch(typeof(PreviewTalkView), nameof(PreviewTalkView.RefreshTalk))]
    internal static class PreviewTalkRefreshLifecyclePatch
    {
        private static void Prefix(PreviewTalkView __instance, int __0)
        {
            BetterMusicController.Instance?.BeforeTalkRefresh(__instance, __0);
        }

        private static void Postfix(PreviewTalkView __instance)
        {
            int currentTalkId = PreviewTalkAccess.GetCurrentCfg(__instance)?.id ?? 0;
            BetterMusicController.Instance?.AfterTalkRefresh(__instance, currentTalkId);
        }
    }

    [HarmonyPatch(typeof(NewTalkView), nameof(NewTalkView.SpeedUp))]
    internal static class NewTalkSpeedUpGuardPatch
    {
        private static bool Prefix(NewTalkView __instance, bool __0)
        {
            if (!__0)
            {
                return true;
            }

            BetterMusicController controller = BetterMusicController.Instance;
            return controller == null || !controller.ShouldBlockFastForward(__instance);
        }
    }

    [HarmonyPatch(typeof(NewTalkView), "OnClickSkip")]
    internal static class NewTalkDirectSkipGuardPatch
    {
        private static bool Prefix(NewTalkView __instance)
        {
            BetterMusicController controller = BetterMusicController.Instance;
            return controller == null || !controller.ShouldBlockFastForward(__instance);
        }
    }

    [HarmonyPatch(typeof(NewTalkView), nameof(NewTalkView.CloseView))]
    internal static class NewTalkCloseGuardPatch
    {
        private static bool Prefix(NewTalkView __instance)
        {
            BetterMusicController controller = BetterMusicController.Instance;
            if (controller != null && controller.ShouldBlockClose(__instance))
            {
                return false;
            }

            controller?.CleanupForView(__instance, "NewTalkView.CloseView");
            return true;
        }
    }

    // PreviewTalkView 没有覆写 CloseView，因此其关闭入口位于 BaseView。
    [HarmonyPatch(typeof(BaseView), nameof(BaseView.CloseView))]
    internal static class PreviewBaseCloseGuardPatch
    {
        private static bool Prefix(BaseView __instance)
        {
            if (!(__instance is PreviewTalkView))
            {
                return true;
            }

            BetterMusicController controller = BetterMusicController.Instance;
            if (controller != null && controller.ShouldBlockClose(__instance))
            {
                return false;
            }

            controller?.CleanupForView(__instance, "PreviewTalkView.CloseView");
            return true;
        }
    }

    [HarmonyPatch(
        typeof(UIMgr),
        nameof(UIMgr.CloseView),
        new Type[] { typeof(BaseView), typeof(bool), typeof(bool) })]
    internal static class TalkUIMgrCloseGuardPatch
    {
        private static bool Prefix(BaseView __0)
        {
            if (!(__0 is NewTalkView) && !(__0 is PreviewTalkView))
            {
                return true;
            }

            BetterMusicController controller = BetterMusicController.Instance;
            if (controller != null && controller.ShouldBlockClose(__0))
            {
                return false;
            }

            controller?.CleanupForView(__0, $"UIMgr.CloseView({__0.GetType().Name})");
            return true;
        }
    }

    [HarmonyPatch(typeof(NewTalkView), nameof(NewTalkView.OnClose))]
    internal static class NewTalkOnCloseCleanupPatch
    {
        private static void Prefix(NewTalkView __instance)
        {
            BetterMusicController.Instance?.CleanupForView(__instance, "NewTalkView.OnClose");
        }
    }

    [HarmonyPatch(typeof(PreviewTalkView), nameof(PreviewTalkView.OnClose))]
    internal static class PreviewTalkOnCloseCleanupPatch
    {
        private static void Prefix(PreviewTalkView __instance)
        {
            BetterMusicController.Instance?.CleanupForView(__instance, "PreviewTalkView.OnClose");
        }
    }
}
