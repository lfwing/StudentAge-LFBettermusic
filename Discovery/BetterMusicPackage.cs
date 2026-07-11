namespace LFBetterMusic.Discovery
{
    /// <summary>
    /// 一个可被 BetterMusic 读取的资源包。对于创意工坊 Mod，BetterMusicDirectory
    /// 指向：&lt;WorkshopItemRoot&gt;/Bettermusic。
    /// </summary>
    public sealed class BetterMusicPackage
    {
        public string ModRootDirectory { get; set; }
        public string BetterMusicDirectory { get; set; }
        public string ConfigPath { get; set; }
        public string SourceLabel { get; set; }
        public string WorkshopItemId { get; set; }
        public bool IsWorkshopPackage { get; set; }
    }
}
