using System.Collections.Generic;
using Effect;
using LFBetterMusic.Runtime;
using Sdk;
using View.Event;
using View.Evt;

namespace LFBetterMusic.Effects
{
    public sealed class EffectorBetterMusic : Effector
    {
        private readonly BetterMusicEffectRequest _request;

        internal EffectorBetterMusic(
            Effector previous,
            List<float> effect,
            BetterMusicEffectRequest request)
            : base(previous, effect)
        {
            _request = request;
        }

        public override void OnRun(float _rate = 1f, bool _toast = false)
        {
            BaseView owner = ResolveRuntimeOwner();
            int talkId = owner is NewTalkView runtimeView
                ? RuntimeTalkAccess.TryGetTalkId(runtimeView)
                : 0;

            BetterMusicController controller = BetterMusicController.EnsureInstance();
            if (controller == null)
            {
                Plugin.LogEffectError("无法创建播放控制器，指令未执行。");
                return;
            }

            controller.ExecuteRequest(
                _request,
                owner,
                owner == null ? TalkChannel.RuntimeStandalone : TalkChannel.Runtime,
                talkId,
                null);
        }

        private static BaseView ResolveRuntimeOwner()
        {
            BaseView normalTalk = UIMgr.GetOpeningView<NewTalkView>() as BaseView;
            if (normalTalk != null)
            {
                return normalTalk;
            }

            return UIMgr.GetOpeningView<CommonTalkView>() as BaseView;
        }

        public override string OnToString(float _rate = 1f, int _type = 0)
        {
            return null;
        }
    }
}
