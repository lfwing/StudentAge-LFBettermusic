using System.Collections.Generic;
using System.IO;
using Config;
using LFBetterMusic.Assets;
using LFBetterMusic.Config;
using LFBetterMusic.Runtime;
using UnityEngine;

namespace LFBetterMusic.Audio
{
    public static class MusicResolver
    {
        public static bool TryResolve(
            int musicId,
            MusicResolveContext context,
            out ResolvedMusic resolved,
            out string error)
        {
            resolved = null;
            error = null;

            // 1163001 永远优先于 JSON 与原版 AudioCfg，属于代码级保留音乐。
            if (BuiltInValidationAssets.TryGetEntry(musicId, out BetterMusicEntry builtIn))
            {
                if (string.IsNullOrWhiteSpace(builtIn.MusicPath) || !File.Exists(builtIn.MusicPath))
                {
                    error = $"内置校验音乐 ID={musicId} 的释放文件不存在：{builtIn.MusicPath}";
                    return false;
                }

                string builtInLrc = !string.IsNullOrWhiteSpace(builtIn.LrcPath) && File.Exists(builtIn.LrcPath)
                    ? builtIn.LrcPath
                    : null;

                resolved = new ResolvedMusic
                {
                    Id = musicId,
                    Name = builtIn.Name,
                    AudioPath = builtIn.MusicPath,
                    LrcPath = builtInLrc,
                    Volume = Mathf.Clamp01(builtIn.Volume),
                    UsesExternalMusicFile = true,
                    UsesOriginalGameAudio = false
                };
                return true;
            }

            BetterMusicEntry entry = null;
            Plugin.ConfigStore?.TryGet(musicId, out entry);

            Dictionary<int, AudioCfg> primaryMap = context?.Channel == TalkChannel.Preview
                ? context.PreviewAudioCfgMap
                : null;

            AudioCfg originalCfg = null;
            if (primaryMap != null)
            {
                primaryMap.TryGetValue(musicId, out originalCfg);
            }
            if (originalCfg == null)
            {
                Cfg.AudioCfgMap?.TryGetValue(musicId, out originalCfg);
            }

            string audioPath = null;
            bool external = false;
            bool original = false;

            if (entry != null && !string.IsNullOrWhiteSpace(entry.MusicPath))
            {
                audioPath = BetterMusicConfigStore.ResolvePath(entry, entry.MusicPath);
                external = true;
                if (!File.Exists(audioPath))
                {
                    error = $"音乐 ID={musicId} 的自定义文件不存在：{audioPath}；来源={entry.SourceLabel}";
                    return false;
                }
            }
            else if (originalCfg != null && !string.IsNullOrWhiteSpace(originalCfg.url))
            {
                // JSON musicPath 为空时，允许给原版音乐补外挂 LRC/音量。
                audioPath = AudioMgrEx.FormatUrl(originalCfg.url);
                original = true;
            }

            if (string.IsNullOrWhiteSpace(audioPath))
            {
                error = $"音乐 ID={musicId} 既没有可用的 JSON musicPath，也不在当前 AudioCfgMap/Cfg.AudioCfgMap 中。";
                return false;
            }

            string lrcPath = null;
            if (entry != null && !string.IsNullOrWhiteSpace(entry.LrcPath))
            {
                lrcPath = BetterMusicConfigStore.ResolvePath(entry, entry.LrcPath);
                if (!File.Exists(lrcPath))
                {
                    lrcPath = null;
                }
            }

            float volume = entry != null
                ? Mathf.Clamp01(entry.Volume)
                : (originalCfg != null ? Mathf.Clamp01(originalCfg.volumn) : 1f);

            resolved = new ResolvedMusic
            {
                Id = musicId,
                Name = !string.IsNullOrWhiteSpace(entry?.Name)
                    ? entry.Name
                    : (!string.IsNullOrWhiteSpace(originalCfg?.name) ? originalCfg.name : $"Music {musicId}"),
                AudioPath = audioPath,
                LrcPath = lrcPath,
                Volume = volume,
                UsesExternalMusicFile = external,
                UsesOriginalGameAudio = original
            };
            return true;
        }
    }
}
