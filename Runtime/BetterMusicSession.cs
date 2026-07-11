using System.Collections.Generic;
using LFBetterMusic.Audio;
using LFBetterMusic.Effects;
using LFBetterMusic.Lyrics;
using Sdk;

namespace LFBetterMusic.Runtime
{
    internal enum TalkAdvanceOrigin
    {
        None,
        Manual,
        Automatic,
        System
    }

    public sealed class BetterMusicSession
    {
        public long Token { get; set; }
        internal BetterMusicEffectRequest Request { get; set; }
        public int MusicId { get; set; }
        internal BetterMusicPlaybackScope Scope { get; set; }
        internal BetterMusicContentKind ContentKind { get; set; }
        public int PlayMode { get; set; }
        public int LyricSizeMode { get; set; }
        public int LyricColorMode { get; set; }
        public bool ShowLyrics { get; set; }
        public bool ShouldLoop { get; set; }
        public int TalkId { get; set; }
        public TalkChannel Channel { get; set; }
        public BaseView OwnerView { get; set; }
        public bool RequiresOwner { get; set; }
        public float MinimumAdvanceAt { get; set; }
        internal TalkAdvanceOrigin PendingAdvanceOrigin { get; set; }
        public ResolvedMusic ResolvedMusic { get; set; }
        public List<LrcLine> Lyrics { get; set; } = new List<LrcLine>();
        internal Dictionary<int, SingingRoleInfo> SingingRoles { get; set; } =
            new Dictionary<int, SingingRoleInfo>();
        public int LyricIndex { get; set; } = -1;
        public float LastPlaybackTime { get; set; } = -1f;
        public float PausedAtSeconds { get; set; }
        public float PlaybackStartSeconds { get; set; }
        public float PlaybackEndSeconds { get; set; }
        public bool HasPlaybackEndBoundary { get; set; }
        public bool UsesManualSegmentLoop { get; set; }
        public bool IsLoading { get; set; }
        public bool IsPlaying { get; set; }
        public bool IsPaused { get; set; }
        public bool IsCancelled { get; set; }

        internal bool IsSinging => ContentKind == BetterMusicContentKind.Singing;
        internal bool BlocksBackgroundAutomaticAdvance =>
            Scope == BetterMusicPlaybackScope.Background &&
            ContentKind == BetterMusicContentKind.Singing;
    }
}
