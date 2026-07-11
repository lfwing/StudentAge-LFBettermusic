using System.Collections.Generic;
using Config;
using LFBetterMusic.Effects;
using LFBetterMusic.Runtime;
using View.Evt;

namespace LFBetterMusic.Preview
{
    internal static class BetterMusicPreviewBridge
    {
        public static void TryTriggerCurrentTalk(PreviewTalkView view)
        {
            if (view == null || view.talkState != TalkState.Anim)
            {
                return;
            }

            TalkCfg cfg = PreviewTalkAccess.GetCurrentCfg(view);
            if (cfg == null || cfg.id == PreviewTalkAccess.GetLastEffectCfgId(view))
            {
                return;
            }

            TriggerOnly1163(view, cfg);
        }

        public static void TryTriggerEmptyTalkBeforeRefresh(PreviewTalkView view, int talkId)
        {
            Dictionary<int, TalkCfg> map = PreviewTalkAccess.GetTalkCfgMap(view);
            if (map == null || !map.TryGetValue(talkId, out TalkCfg cfg) || cfg == null)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(cfg.content) || cfg.id == PreviewTalkAccess.GetLastEffectCfgId(view))
            {
                return;
            }

            TriggerOnly1163(view, cfg);
        }

        private static void TriggerOnly1163(PreviewTalkView view, TalkCfg cfg)
        {
            if (cfg.effect == null)
            {
                return;
            }

            foreach (List<float> effect in cfg.effect)
            {
                bool isCustom = BetterMusicEffectEncoding.TryParse(
                    effect,
                    out BetterMusicEffectRequest request,
                    out string parseError);

                if (!isCustom)
                {
                    continue;
                }

                if (parseError != null)
                {
                    Plugin.LogEffectError(parseError, effect);
                    continue;
                }

                BetterMusicController.EnsureInstance()?.ExecuteRequest(
                    request,
                    view,
                    TalkChannel.Preview,
                    cfg.id,
                    PreviewTalkAccess.GetAudioCfgMap(view),
                    PreviewTalkAccess.GetPersonCfgMap(view),
                    PreviewTalkAccess.GetGender(view));
            }
        }
    }
}
