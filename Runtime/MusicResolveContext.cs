using System.Collections.Generic;
using Config;

namespace LFBetterMusic.Runtime
{
    public sealed class MusicResolveContext
    {
        public TalkChannel Channel { get; set; }
        public Dictionary<int, AudioCfg> PreviewAudioCfgMap { get; set; }
    }
}
