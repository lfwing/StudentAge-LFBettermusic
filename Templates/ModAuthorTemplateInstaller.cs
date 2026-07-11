using System;
using System.IO;
using System.Text;

namespace LFBetterMusic.Templates
{
    /// <summary>
    /// 在 BepInEx/plugins/Bettermusic 下维护一份“仅供复制”的 Mod 作者模板。
    /// 该目录绝不参与 BetterMusic 资源注册，也不是插件 DLL 的强制安装目录。
    /// </summary>
    internal static class ModAuthorTemplateInstaller
    {
        internal const string TemplateFolderName = "Bettermusic";

        private static readonly UTF8Encoding Utf8NoBom = new UTF8Encoding(false);

        internal static void EnsureInstalled(string templateDirectory)
        {
            if (string.IsNullOrWhiteSpace(templateDirectory))
            {
                throw new ArgumentException("templateDirectory 不能为空。", nameof(templateDirectory));
            }

            string root = Path.GetFullPath(templateDirectory);
            string musicDirectory = Path.Combine(root, "Music");
            string lyricsDirectory = Path.Combine(root, "Lyrics");

            Directory.CreateDirectory(root);
            Directory.CreateDirectory(musicDirectory);
            Directory.CreateDirectory(lyricsDirectory);

            WriteIfMissing(
                Path.Combine(root, "Bettermusic.json"),
                CreateTemplateJson());

            WriteIfMissing(
                Path.Combine(musicDirectory, "README.txt"),
                "把 Mod 自定义音乐文件放在这里。\n" +
                "Bettermusic.json 中建议使用相对路径，例如：\n" +
                "  \"musicPath\": \"Music/MySong.mp3\"\n\n" +
                "支持格式取决于游戏当前 ResMgr.LoadAudioAsync / Unity 音频解码能力。\n");

            WriteIfMissing(
                Path.Combine(lyricsDirectory, "example.lrc"),
                "[00:00.00]第一句示例歌词\n" +
                "[00:03.50]第二句示例歌词\n" +
                "[00:07.0]第三句示例歌词\n");

            WriteIfMissing(
                Path.Combine(lyricsDirectory, "original_12345.lrc"),
                "[00:00.00]把此文件替换为原版音乐对应的 LRC\n");
        }

        private static void WriteIfMissing(string path, string content)
        {
            if (File.Exists(path))
            {
                return;
            }

            File.WriteAllText(path, content ?? string.Empty, Utf8NoBom);
        }

        private static string CreateTemplateJson()
        {
            return
                "{\n" +
                "  \"musics\": [\n" +
                "    {\n" +
                "      \"id\": 90001,\n" +
                "      \"name\": \"自定义音乐示例\",\n" +
                "      \"musicPath\": \"Music/example.mp3\",\n" +
                "      \"lrcPath\": \"Lyrics/example.lrc\",\n" +
                "      \"volume\": 0.85\n" +
                "    },\n" +
                "    {\n" +
                "      \"id\": 12345,\n" +
                "      \"name\": \"原版音乐外挂歌词示例（请改成真实原版音乐 ID）\",\n" +
                "      \"musicPath\": \"\",\n" +
                "      \"lrcPath\": \"Lyrics/original_12345.lrc\",\n" +
                "      \"volume\": 1.0\n" +
                "    }\n" +
                "  ]\n" +
                "}\n";
        }
    }
}
