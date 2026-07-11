using System.Collections.Generic;
using Newtonsoft.Json;

namespace LFBetterMusic.Config
{
    public sealed class BetterMusicConfigRoot
    {
        [JsonProperty("musics")]
        public List<BetterMusicEntry> Musics { get; set; } = new List<BetterMusicEntry>();
    }

    public sealed class BetterMusicEntry
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("musicPath")]
        public string MusicPath { get; set; }

        [JsonProperty("lrcPath")]
        public string LrcPath { get; set; }

        [JsonProperty("volume")]
        public float Volume { get; set; } = 1f;

        // 以下字段只在运行时记录“这个条目来自哪个 Mod”，不会写回 JSON。
        // 相对路径必须相对该条目自己的 Bettermusic 目录解析，不能统一相对 BepInEx。
        [JsonIgnore]
        public string SourceDirectory { get; set; }

        [JsonIgnore]
        public string SourceConfigPath { get; set; }

        [JsonIgnore]
        public string SourceLabel { get; set; }
    }
}
