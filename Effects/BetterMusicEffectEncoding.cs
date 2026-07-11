using System;
using System.Collections.Generic;
using System.Globalization;

namespace LFBetterMusic.Effects
{
    internal enum BetterMusicCommandKind
    {
        StopAndRefresh,
        Play,
        PauseOrResume
    }

    internal enum BetterMusicPlaybackScope
    {
        SingleTalk,
        Background
    }

    internal enum BetterMusicContentKind
    {
        BackgroundMusic,
        Singing
    }

    internal sealed class BetterMusicEffectRequest
    {
        internal BetterMusicCommandKind Command { get; set; }
        internal BetterMusicPlaybackScope Scope { get; set; }
        internal BetterMusicContentKind ContentKind { get; set; }
        internal int SourceSubcommand { get; set; }
        internal int MusicId { get; set; }
        internal int PlayMode { get; set; }
        internal int LyricSizeMode { get; set; }
        internal int LyricColorMode { get; set; }
        internal int PauseAction { get; set; }
        internal IReadOnlyList<int> SingerRoleIds { get; set; } = Array.Empty<int>();
        internal bool HasStartLine { get; set; }
        internal int StartLine { get; set; }
        internal bool HasEndLine { get; set; }
        internal int EndLine { get; set; }

        internal bool ShowLyrics => LyricSizeMode != -1;
        internal bool ShouldLoop => PlayMode == 2;
        internal bool IsSinging => ContentKind == BetterMusicContentKind.Singing;
    }

    internal static class BetterMusicEffectEncoding
    {
        internal const float DirectFactoryId = 1163f;

        internal static bool HasCustomMarker(IReadOnlyList<float> effect)
        {
            return effect != null && effect.Count > 0 && effect[0] == DirectFactoryId;
        }

        internal static bool TryParse(
            IReadOnlyList<float> effect,
            out BetterMusicEffectRequest request,
            out string error)
        {
            request = null;
            error = null;

            if (!HasCustomMarker(effect))
            {
                return false;
            }

            if (effect.Count < 2)
            {
                error = "1163 参数不足：缺少子指令。";
                return true;
            }

            int subcommand = (int)effect[1];
            switch (subcommand)
            {
                case 0:
                    request = new BetterMusicEffectRequest
                    {
                        Command = BetterMusicCommandKind.StopAndRefresh,
                        SourceSubcommand = 0
                    };
                    return true;

                case 1:
                    return TryParseBackgroundMusicPlayback(
                        effect,
                        BetterMusicPlaybackScope.SingleTalk,
                        1,
                        out request,
                        out error);

                case 2:
                    return TryParseSingingPlayback(
                        effect,
                        BetterMusicPlaybackScope.SingleTalk,
                        2,
                        out request,
                        out error);

                case 10:
                    return TryParseBackgroundMusicPlayback(
                        effect,
                        BetterMusicPlaybackScope.Background,
                        10,
                        out request,
                        out error);

                case 20:
                    return TryParseSingingPlayback(
                        effect,
                        BetterMusicPlaybackScope.Background,
                        20,
                        out request,
                        out error);

                case 99:
                    if (effect.Count < 3)
                    {
                        error = "1163,99 格式必须为：1163,99,1 或 1163,99,2。";
                        return true;
                    }

                    int pauseAction = (int)effect[2];
                    if (pauseAction != 1 && pauseAction != 2)
                    {
                        error = $"1163,99 参数 x={pauseAction} 无效，只允许 1（暂停）或 2（恢复）。";
                        return true;
                    }

                    request = new BetterMusicEffectRequest
                    {
                        Command = BetterMusicCommandKind.PauseOrResume,
                        SourceSubcommand = 99,
                        PauseAction = pauseAction
                    };
                    return true;

                default:
                    error = $"1163 子指令 {subcommand} 无效。当前可用：0、1、2、10、20、99。";
                    return true;
            }
        }

        private static bool TryParseBackgroundMusicPlayback(
            IReadOnlyList<float> effect,
            BetterMusicPlaybackScope scope,
            int subcommand,
            out BetterMusicEffectRequest request,
            out string error)
        {
            request = null;
            error = null;

            if (effect.Count < 6)
            {
                error = $"1163,{subcommand} 格式参数不足，必须为：" +
                        $"1163,{subcommand},音乐ID,播放类型,歌词字号,歌词颜色[,u[,v]]。";
                return true;
            }

            if (effect.Count > 8)
            {
                error = $"1163,{subcommand} 扩展参数过多，只允许可选的 u、v。";
                return true;
            }

            int musicId = (int)effect[2];
            if (musicId <= 0)
            {
                error = $"1163,{subcommand} 音乐 ID x={musicId} 无效，必须大于 0。";
                return true;
            }

            int playMode = (int)effect[3];
            if (scope == BetterMusicPlaybackScope.SingleTalk)
            {
                if (playMode < 1 || playMode > 3)
                {
                    error = $"1163,1 播放类型 y={playMode} 无效，只允许 1、2、3。";
                    return true;
                }
            }
            else
            {
                // 针对背景的背景型音乐固定循环。
                playMode = 2;
            }

            int lyricSizeMode = NormalizeBackgroundLyricSize((int)effect[4]);
            int lyricColorMode = lyricSizeMode == -1
                ? 0
                : NormalizeLyricColor((int)effect[5]);

            request = new BetterMusicEffectRequest
            {
                Command = BetterMusicCommandKind.Play,
                Scope = scope,
                ContentKind = BetterMusicContentKind.BackgroundMusic,
                SourceSubcommand = subcommand,
                MusicId = musicId,
                PlayMode = playMode,
                LyricSizeMode = lyricSizeMode,
                LyricColorMode = lyricColorMode
            };

            return TryParseLineRange(effect, 6, request, out error);
        }

        private static bool TryParseSingingPlayback(
            IReadOnlyList<float> effect,
            BetterMusicPlaybackScope scope,
            int subcommand,
            out BetterMusicEffectRequest request,
            out string error)
        {
            request = null;
            error = null;

            if (effect.Count < 6)
            {
                error = $"1163,{subcommand} 格式参数不足，必须为：" +
                        $"1163,{subcommand},音乐ID,播放类型,歌词字号,角色ID...,-1[,u[,v]]。";
                return true;
            }

            int musicId = (int)effect[2];
            if (musicId <= 0)
            {
                error = $"1163,{subcommand} 音乐 ID x={musicId} 无效，必须大于 0。";
                return true;
            }

            int terminatorIndex = -1;
            var singerRoleIds = new List<int>();
            for (int i = 5; i < effect.Count; i++)
            {
                int roleId = (int)effect[i];
                if (roleId == -1)
                {
                    terminatorIndex = i;
                    break;
                }

                if (roleId < 0)
                {
                    error = $"1163,{subcommand} 演唱角色 ID={roleId} 无效；角色 ID 必须为非负数，并以 -1 结束。";
                    return true;
                }

                singerRoleIds.Add(roleId);
            }

            if (terminatorIndex < 0)
            {
                error = $"1163,{subcommand} 缺少演唱角色终止符 -1。";
                return true;
            }

            if (effect.Count - terminatorIndex - 1 > 2)
            {
                error = $"1163,{subcommand} 在 -1 后只允许可选的 u、v。";
                return true;
            }

            request = new BetterMusicEffectRequest
            {
                Command = BetterMusicCommandKind.Play,
                Scope = scope,
                ContentKind = BetterMusicContentKind.Singing,
                SourceSubcommand = subcommand,
                MusicId = musicId,
                // 单 Talk 唱歌固定为 3；背景唱歌固定为 1。
                PlayMode = scope == BetterMusicPlaybackScope.SingleTalk ? 3 : 1,
                LyricSizeMode = NormalizeSingingLyricSize((int)effect[4]),
                LyricColorMode = 0,
                SingerRoleIds = singerRoleIds
            };

            return TryParseLineRange(effect, terminatorIndex + 1, request, out error);
        }

        private static bool TryParseLineRange(
            IReadOnlyList<float> effect,
            int startIndex,
            BetterMusicEffectRequest request,
            out string error)
        {
            error = null;
            int count = effect.Count - startIndex;
            if (count <= 0)
            {
                return true;
            }

            int u = (int)effect[startIndex];
            if (u < 0)
            {
                error = $"1163,{request.SourceSubcommand} 的 u={u} 无效，u 必须为非负数。";
                return true;
            }

            request.HasStartLine = true;
            request.StartLine = u;

            if (count == 1)
            {
                return true;
            }

            int v = (int)effect[startIndex + 1];
            if (v < u)
            {
                error = $"1163,{request.SourceSubcommand} 的 v={v} 无效，v 必须大于等于 u={u}。";
                return true;
            }

            // 0 仅作为 u 的“从音频开头播放”标记；不存在第 0 句歌词。
            if (v == 0)
            {
                error = $"1163,{request.SourceSubcommand} 的 v=0 无效；v 必须指向第 1 句或之后的歌词。";
                return true;
            }

            request.HasEndLine = true;
            request.EndLine = v;
            return true;
        }

        private static int NormalizeBackgroundLyricSize(int value)
        {
            return value == -1 || (value >= 1 && value <= 4) ? value : -1;
        }

        private static int NormalizeSingingLyricSize(int value)
        {
            return value >= 1 && value <= 4 ? value : 1;
        }

        private static int NormalizeLyricColor(int value)
        {
            return value >= 0 && value <= 12 ? value : 0;
        }

        internal static string Format(IReadOnlyList<float> effect)
        {
            if (effect == null)
            {
                return "<null>";
            }

            var parts = new string[effect.Count];
            for (int i = 0; i < effect.Count; i++)
            {
                parts[i] = effect[i].ToString("0.###", CultureInfo.InvariantCulture);
            }
            return string.Join(",", parts);
        }
    }
}
