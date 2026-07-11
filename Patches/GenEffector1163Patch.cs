using System.Collections.Generic;
using Effect;
using LFBetterMusic.Effects;

namespace LFBetterMusic.Patches
{
    /// <summary>
    /// 正常游戏 EFFECT 工厂的精确入口。
    /// 此类不使用 HarmonyPatch Attribute，由 Plugin 手动安装并由持久控制器保活。
    /// </summary>
    internal static class GenEffector1163Patch
    {
        internal static bool Prefix(
            List<float> __0,
            Effector __1,
            int __2,
            int __3,
            ref Effector __result)
        {
            bool isCustom = BetterMusicEffectEncoding.TryParse(
                __0,
                out BetterMusicEffectRequest request,
                out string error);

            if (!isCustom)
            {
                return true;
            }

            if (error != null)
            {
                Plugin.LogEffectError(error, __0);
                // 当前 1163 无效时保留前面已经构造好的 Effector 链。
                __result = __1;
                return false;
            }

            __result = new EffectorBetterMusic(__1, __0, request)
            {
                toRoleId = __2,
                fromRoleId = __3
            };
            return false;
        }
    }
}
