namespace LFBetterMusic.Audio
{
    public sealed class ResolvedMusic
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string AudioPath { get; set; }
        public string LrcPath { get; set; }
        public float Volume { get; set; }
        public bool UsesExternalMusicFile { get; set; }
        public bool UsesOriginalGameAudio { get; set; }
    }
}
