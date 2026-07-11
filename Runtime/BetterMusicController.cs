using System;
using System.Collections.Generic;
using Config;
using GenUI.Talk;
using LFBetterMusic.Assets;
using LFBetterMusic.Audio;
using LFBetterMusic.Effects;
using LFBetterMusic.Lyrics;
using LFBetterMusic.UI;
using Sdk;
using UnityEngine;
using View.Evt;

namespace LFBetterMusic.Runtime
{
    public sealed class BetterMusicController : MonoBehaviour
    {
        public static BetterMusicController Instance { get; private set; }

        private const float SingleTalkMinimumHoldSeconds = 0.4f;
        private const float HoldToSkipSeconds = 1f;
        private const int HoldToSkipHotKey = 128;

        private AudioSource _audioSource;
        private GameObject _audioObject;
        private BetterMusicSession _session;
        private FloatingLyricsOverlay _lyricsOverlay;
        private HoldToSkipOverlay _holdToSkipOverlay;
        private long _tokenCounter;

        // 插件音乐与原版 BGM 使用两个独立 AudioSource。
        // 仅当插件音乐实际播放时持有原版 BGM 的“优先级租约”；
        // 插件音乐暂停、停止、失败、自然结束或 Talk 中断时必须立即归还。
        private bool _gameMusicSuppressionHeld;
        private bool _resumeGameMusicWhenReleased;
        private bool _gameMusicWarningLogged;
        private AudioSource _suppressedGameMusicSource;

        private BaseView _advanceContextOwner;
        private TalkAdvanceOrigin _advanceContextOrigin;
        private int _advanceContextDepth;

        private BaseView _scheduledAdvanceOwner;
        private TalkAdvanceOrigin _scheduledAdvanceOrigin;
        private float _scheduledAdvanceAt;

        private BaseView _holdOwner;
        private float _holdProgress;
        private bool _applicationQuitting;

        public BetterMusicSession ActiveSession => _session;

        internal static BetterMusicController EnsureInstance()
        {
            if (IsControllerUsable(Instance))
            {
                return Instance;
            }

            try
            {
                Instance = null;
                var go = new GameObject("lf-BetterMusicController");
                DontDestroyOnLoad(go);
                return go.AddComponent<BetterMusicController>();
            }
            catch
            {
                return null;
            }
        }

        private void Awake()
        {
            if (IsControllerUsable(Instance) && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            _lyricsOverlay = new FloatingLyricsOverlay();
            _holdToSkipOverlay = new HoldToSkipOverlay();
        }

        private void Update()
        {
            // BaseUnityPlugin 组件可能在游戏启动阶段被销毁；
            // 持久控制器继续维持正常游戏与编辑器的关键 EFFECT Patch。
            Plugin.Instance?.TickPatchHealthFromPersistentController();
            ProcessScheduledAdvance();
            _lyricsOverlay?.Tick(Time.unscaledDeltaTime);

            BetterMusicSession session = _session;
            if (session == null || session.IsCancelled)
            {
                ReleaseGameMusicSuppression();
                return;
            }

            if (session.RequiresOwner && !IsOwnerAlive(session.OwnerView))
            {
                EndActiveSessionInternal("单Talk Owner 已失效", true, true, false);
                return;
            }

            if (!session.RequiresOwner && session.OwnerView != null && !IsOwnerAlive(session.OwnerView))
            {
                session.OwnerView = null;
                _lyricsOverlay?.Destroy();
            }

            // 加载中没有插件音乐实际发声；暂停/中断时也必须让原版 BGM 正常播放。
            if (session.IsLoading || session.IsPaused || !session.IsPlaying)
            {
                ReleaseGameMusicSuppression();
                return;
            }

            MaintainGameMusicSuppression();

            AudioSource source = GetUsableAudioSource();
            if (source == null || source.clip == null)
            {
                FailActiveSession("AudioSource/clip 在播放中失效");
                return;
            }

            try
            {
                if (HasReachedPlaybackEnd(session, source))
                {
                    if (session.ShouldLoop)
                    {
                        RestartLoop(session, source);
                    }
                    else
                    {
                        CompleteOneShot(session);
                    }
                    return;
                }

                if (session.ShowLyrics)
                {
                    UpdateLyrics(session, source);
                }

                if (!source.isPlaying)
                {
                    if (session.ShouldLoop)
                    {
                        RestartLoop(session, source);
                    }
                    else
                    {
                        CompleteOneShot(session);
                    }
                }
            }
            catch (Exception ex)
            {
                InvalidateAudioSource("Update访问失败");
                FailActiveSession($"播放控制器运行异常：{ex.Message}");
            }
        }

        internal void ExecuteRequest(
            BetterMusicEffectRequest request,
            BaseView owner,
            TalkChannel channel,
            int talkId,
            Dictionary<int, AudioCfg> previewAudioCfgMap = null,
            Dictionary<int, PersonCfg> previewPersonCfgMap = null,
            GenderDefine previewGender = GenderDefine.Unknown)
        {
            if (request == null)
            {
                return;
            }

            switch (request.Command)
            {
                case BetterMusicCommandKind.StopAndRefresh:
                    StopAndRefreshChannel("1163,0");
                    break;

                case BetterMusicCommandKind.Play:
                    StartSession(
                        request,
                        owner,
                        channel,
                        talkId,
                        previewAudioCfgMap,
                        previewPersonCfgMap,
                        previewGender);
                    break;

                case BetterMusicCommandKind.PauseOrResume:
                    if (request.PauseAction == 1)
                    {
                        PauseActiveMusic();
                    }
                    else
                    {
                        ResumeActiveMusic();
                    }
                    break;
            }
        }

        private void StartSession(
            BetterMusicEffectRequest request,
            BaseView owner,
            TalkChannel channel,
            int talkId,
            Dictionary<int, AudioCfg> previewAudioCfgMap,
            Dictionary<int, PersonCfg> previewPersonCfgMap,
            GenderDefine previewGender)
        {
            bool singleTalk = request.Scope == BetterMusicPlaybackScope.SingleTalk;
            if (singleTalk && owner == null)
            {
                Plugin.LogEffectError(
                    $"1163,{request.SourceSubcommand} 当前没有可绑定的 Talk，单一talk音乐未执行。");
                return;
            }

            var context = new MusicResolveContext
            {
                Channel = channel,
                PreviewAudioCfgMap = previewAudioCfgMap
            };

            if (!MusicResolver.TryResolve(
                    request.MusicId,
                    context,
                    out ResolvedMusic resolved,
                    out string error))
            {
                Plugin.LogEffectError(error);
                return;
            }

            long token = _tokenCounter + 1;
            var session = new BetterMusicSession
            {
                Token = token,
                Request = request,
                MusicId = request.MusicId,
                Scope = request.Scope,
                ContentKind = request.ContentKind,
                PlayMode = request.PlayMode,
                LyricSizeMode = request.LyricSizeMode,
                LyricColorMode = request.LyricColorMode,
                ShowLyrics = request.ShowLyrics,
                ShouldLoop = request.ShouldLoop,
                TalkId = talkId,
                Channel = channel,
                OwnerView = owner,
                RequiresOwner = singleTalk,
                MinimumAdvanceAt = singleTalk
                    ? Time.unscaledTime + SingleTalkMinimumHoldSeconds
                    : 0f,
                ResolvedMusic = resolved,
                IsLoading = true
            };

            bool needsLyricsForRange = request.HasStartLine || request.HasEndLine;
            bool shouldParseLyrics = session.ShowLyrics || needsLyricsForRange || session.IsSinging;
            if (shouldParseLyrics && !string.IsNullOrWhiteSpace(resolved.LrcPath))
            {
                try
                {
                    session.Lyrics = LrcParser.ParseFile(resolved.LrcPath);
                }
                catch (Exception ex)
                {
                    Plugin.LogEffectError(
                        $"音乐 ID={request.MusicId} 的 LRC 解析失败：{ex.Message}");
                    session.Lyrics = new List<LrcLine>();
                }
            }

            if (needsLyricsForRange && session.Lyrics.Count == 0)
            {
                Plugin.LogEffectError(
                    $"1163,{request.SourceSubcommand} 使用 u/v 必须存在至少一条有效时间歌词，指令未执行。");
                return;
            }

            if (!ValidateRequestedLineRange(request, session.Lyrics, out string rangeError))
            {
                Plugin.LogEffectError(rangeError);
                return;
            }

            if (session.IsSinging)
            {
                var roleIssues = new List<string>();
                session.SingingRoles = SingingRoleResolver.Resolve(
                    request.SingerRoleIds,
                    channel,
                    previewPersonCfgMap,
                    previewGender,
                    roleIssues);

                foreach (string roleIssue in roleIssues)
                {
                    Plugin.LogEffectError(roleIssue);
                }
            }

            // 到此说明参数、资源和 LRC 均通过；无效的新指令不会破坏旧会话。
            EndActiveSessionInternal("新音乐替换旧会话", true, true, false);
            token = ++_tokenCounter;
            session.Token = token;
            _session = session;

            if (singleTalk)
            {
                DisableFastForwardNow(owner);
            }

            AttachLyricsIfPossible(session, owner);

            AudioSource source = EnsureAudioSource("开始新会话");
            if (source == null)
            {
                FailActiveSession("无法创建 AudioSource");
                return;
            }

            TryBindGameMusicMixer(source);

            ResMgr.LoadAudioAsync(resolved.AudioPath, clip =>
            {
                try
                {
                    HandleLoadedClip(token, clip);
                }
                catch (Exception ex)
                {
                    if (_session != null && _session.Token == token)
                    {
                        FailActiveSession($"音频异步回调失败：{ex.Message}");
                    }
                }
            }, null, false);
        }

        private static bool ValidateRequestedLineRange(
            BetterMusicEffectRequest request,
            IList<LrcLine> lyrics,
            out string error)
        {
            error = null;
            if (!request.HasStartLine && !request.HasEndLine)
            {
                return true;
            }

            int lyricCount = lyrics?.Count ?? 0;
            if (request.StartLine > lyricCount)
            {
                error = $"[1163,{request.SourceSubcommand}] u={request.StartLine} 大于有效歌词总数 {lyricCount}，指令未执行。";
                return false;
            }

            if (request.HasEndLine && request.EndLine > lyricCount)
            {
                error = $"[1163,{request.SourceSubcommand}] v={request.EndLine} 大于有效歌词总数 {lyricCount}，指令未执行。";
                return false;
            }

            return true;
        }

        private void HandleLoadedClip(long token, AudioClip clip)
        {
            BetterMusicSession session = _session;
            if (session == null || session.Token != token || session.IsCancelled)
            {
                return;
            }

            if (session.RequiresOwner && !IsOwnerAlive(session.OwnerView))
            {
                EndActiveSessionInternal("加载完成前 Talk 已关闭", true, true, false);
                return;
            }

            if (clip == null)
            {
                FailActiveSession("音乐加载失败");
                return;
            }

            if (!ConfigurePlaybackWindow(session, clip, out string windowError))
            {
                FailActiveSession(windowError);
                return;
            }

            // 到音频即将真正发声时才取得优先级；加载期间原版 BGM 保持正常。
            AcquireGameMusicSuppression();
            if (!TryStartClipWithRecovery(session, clip, out string error))
            {
                FailActiveSession($"插件音乐播放失败：{error}");
                return;
            }

            session.IsLoading = false;
            session.IsPlaying = true;
            session.IsPaused = false;
            ResetLyrics(session);
            MaintainGameMusicSuppression();

            LogPlaybackSuccess(session);
        }

        private static void LogPlaybackSuccess(BetterMusicSession session)
        {
            if (session == null || session.Request == null)
            {
                return;
            }

            BetterMusicEffectRequest request = session.Request;
            string typeName = request.Scope == BetterMusicPlaybackScope.SingleTalk
                ? request.IsSinging
                    ? "针对单一talk的唱歌型音乐"
                    : "针对单一talk的背景型音乐"
                : request.IsSinging
                    ? "针对背景的唱歌型音乐"
                    : "针对背景的背景型音乐";

            string musicName = session.ResolvedMusic != null &&
                               !string.IsNullOrWhiteSpace(session.ResolvedMusic.Name)
                ? session.ResolvedMusic.Name
                : "未知音乐";

            string detail =
                $"类型={typeName}；音乐={musicName}（ID={session.MusicId}）；" +
                $"字号={FormatLyricSize(request.LyricSizeMode)}";

            if (request.IsSinging)
            {
                detail += $"；唱歌人={FormatSingingRoles(session)}";
            }
            else
            {
                detail += $"；歌词颜色={FormatLyricColor(request)}";
            }

            if (request.HasStartLine)
            {
                detail += $"；u={request.StartLine}";
                if (request.HasEndLine)
                {
                    detail += $"，v={request.EndLine}";
                }
                else
                {
                    detail += "，播放至结束";
                }
            }

            Plugin.LogEffectSuccess(detail + "。");
        }

        private static string FormatLyricSize(int sizeMode)
        {
            switch (sizeMode)
            {
                case -1:
                    return "-1（不显示歌词）";
                case 2:
                    return "2（1.2倍）";
                case 3:
                    return "3（1.5倍）";
                case 4:
                    return "4（1.8倍）";
                default:
                    return "1（默认）";
            }
        }

        private static string FormatLyricColor(BetterMusicEffectRequest request)
        {
            if (request == null || !request.ShowLyrics)
            {
                return "不显示歌词";
            }

            string[] names =
            {
                "白色", "红色", "橙红色", "橙色", "橙黄色", "黄色",
                "黄绿色", "绿色", "蓝绿色", "蓝色", "蓝紫色", "紫色", "紫红色"
            };
            int index = Mathf.Clamp(request.LyricColorMode, 0, names.Length - 1);
            return $"{request.LyricColorMode}（{names[index]}）";
        }

        private static string FormatSingingRoles(BetterMusicSession session)
        {
            BetterMusicEffectRequest request = session?.Request;
            if (request?.SingerRoleIds == null || request.SingerRoleIds.Count == 0)
            {
                return "未指定，按合唱处理";
            }

            var parts = new List<string>();
            for (int slot = 1; slot <= request.SingerRoleIds.Count; slot++)
            {
                int requestedRoleId = request.SingerRoleIds[slot - 1];
                SingingRoleInfo role = null;
                session.SingingRoles?.TryGetValue(slot, out role);

                string name = role?.Name ?? "合唱";
                string gender = FormatGender(role?.Gender ?? GenderDefine.Unknown);
                int resolvedRoleId = role != null ? role.RoleId : requestedRoleId;
                string roleIdText = resolvedRoleId < 0
                    ? $"原roleId={requestedRoleId}"
                    : $"roleId={resolvedRoleId}";
                parts.Add($"id{slot}={name}（{roleIdText}，{gender}）");
            }

            return string.Join("，", parts);
        }

        private static string FormatGender(GenderDefine gender)
        {
            if (gender == GenderDefine.Male)
            {
                return "男性";
            }
            if (gender == GenderDefine.Female)
            {
                return "女性";
            }
            return "合唱";
        }

        private static bool ConfigurePlaybackWindow(
            BetterMusicSession session,
            AudioClip clip,
            out string error)
        {
            error = null;
            session.PlaybackStartSeconds = 0f;
            session.PlaybackEndSeconds = clip.length;
            session.HasPlaybackEndBoundary = false;
            session.UsesManualSegmentLoop = false;

            BetterMusicEffectRequest request = session.Request;
            if (request == null || (!request.HasStartLine && !request.HasEndLine))
            {
                return true;
            }

            if (request.HasStartLine && request.StartLine > 0)
            {
                LrcLine startLine = FindLyricByLineNumber(session.Lyrics, request.StartLine);
                if (startLine == null)
                {
                    error = $"[1163,{request.SourceSubcommand}] 找不到起始歌词 u={request.StartLine}。";
                    return false;
                }
                session.PlaybackStartSeconds = startLine.TimeSeconds;
            }

            if (request.HasEndLine)
            {
                LrcLine selectedLine = FindLyricByLineNumber(session.Lyrics, request.EndLine);
                if (selectedLine == null)
                {
                    error = $"[1163,{request.SourceSubcommand}] 找不到终止歌词 v={request.EndLine}。";
                    return false;
                }

                float selectedLineTime = selectedLine.TimeSeconds;
                if (selectedLineTime >= clip.length - 0.005f)
                {
                    error = $"[1163,{request.SourceSubcommand}] 终止歌词 v={request.EndLine} 的时间 " +
                            $"{selectedLineTime:F3}s 超出音频长度 {clip.length:F3}s。";
                    return false;
                }

                // v 是包含式终点。句号按 LRC 文件从上到下生成；
                // 实际停止点取 v 之后、时间晚于 v 的第一条有效歌词。
                float end = clip.length;
                foreach (LrcLine candidate in session.Lyrics)
                {
                    if (candidate.LineNumber > request.EndLine &&
                        candidate.TimeSeconds > selectedLineTime + 0.001f &&
                        candidate.TimeSeconds < end)
                    {
                        end = candidate.TimeSeconds;
                    }
                }

                session.PlaybackEndSeconds = Mathf.Min(clip.length, end);
                session.HasPlaybackEndBoundary =
                    session.PlaybackEndSeconds < clip.length - 0.005f;
            }

            if (session.PlaybackStartSeconds < 0f ||
                session.PlaybackStartSeconds >= clip.length - 0.005f)
            {
                error = $"[1163,{request.SourceSubcommand}] 起始歌词时间 {session.PlaybackStartSeconds:F3}s 超出音频长度 {clip.length:F3}s。";
                return false;
            }

            if (session.PlaybackEndSeconds <= session.PlaybackStartSeconds + 0.005f)
            {
                error = $"[1163,{request.SourceSubcommand}] 播放区间为空：" +
                        $"start={session.PlaybackStartSeconds:F3}s，end={session.PlaybackEndSeconds:F3}s。";
                return false;
            }

            session.UsesManualSegmentLoop = session.ShouldLoop &&
                (session.PlaybackStartSeconds > 0.001f || session.HasPlaybackEndBoundary);
            return true;
        }

        private static LrcLine FindLyricByLineNumber(
            IList<LrcLine> lyrics,
            int lineNumber)
        {
            if (lyrics == null || lineNumber <= 0)
            {
                return null;
            }

            for (int i = 0; i < lyrics.Count; i++)
            {
                if (lyrics[i] != null && lyrics[i].LineNumber == lineNumber)
                {
                    return lyrics[i];
                }
            }

            return null;
        }

        private bool TryStartClipWithRecovery(
            BetterMusicSession session,
            AudioClip clip,
            out string error)
        {
            error = null;
            Exception lastException = null;

            for (int attempt = 1; attempt <= 2; attempt++)
            {
                AudioSource source = EnsureAudioSource(
                    attempt == 1 ? "音频加载完成" : "首次启动失败后重建",
                    attempt == 2);

                if (source == null)
                {
                    continue;
                }

                TryBindGameMusicMixer(source);

                try
                {
                    source.Stop();
                    source.clip = clip;
                    source.volume = session.ResolvedMusic.Volume;
                    source.loop = session.ShouldLoop && !session.UsesManualSegmentLoop;
                    source.time = Mathf.Clamp(
                        session.PlaybackStartSeconds,
                        0f,
                        Mathf.Max(0f, clip.length - 0.01f));
                    source.Play();
                    _audioSource = source;
                    return true;
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    InvalidateAudioSource($"第{attempt}次启动失败");
                }
            }

            error = lastException == null
                ? "无法获得可用 AudioSource"
                : $"{lastException.GetType().Name}: {lastException.Message}";
            return false;
        }

        internal void BeginAdvance(BaseView owner, TalkAdvanceOrigin origin)
        {
            if (owner == null)
            {
                return;
            }

            if (_advanceContextDepth == 0)
            {
                _advanceContextOwner = owner;
                _advanceContextOrigin = origin;
            }

            if (ReferenceEquals(_advanceContextOwner, owner))
            {
                _advanceContextDepth++;
            }
        }

        internal void EndAdvance(BaseView owner)
        {
            if (_advanceContextDepth <= 0 || !ReferenceEquals(_advanceContextOwner, owner))
            {
                return;
            }

            _advanceContextDepth--;
            if (_advanceContextDepth == 0)
            {
                _advanceContextOwner = null;
                _advanceContextOrigin = TalkAdvanceOrigin.None;
            }
        }

        private TalkAdvanceOrigin GetAdvanceOrigin(BaseView owner)
        {
            if (_advanceContextDepth > 0 && ReferenceEquals(_advanceContextOwner, owner))
            {
                return _advanceContextOrigin;
            }
            return TalkAdvanceOrigin.System;
        }

        internal bool HandleNextTalkAttempt(BaseView owner)
        {
            BetterMusicSession session = _session;
            if (session != null && !session.IsCancelled &&
                session.BlocksBackgroundAutomaticAdvance)
            {
                TalkAdvanceOrigin backgroundOrigin = GetAdvanceOrigin(owner);
                session.OwnerView = owner;
                if (backgroundOrigin == TalkAdvanceOrigin.Automatic)
                {
                    session.PendingAdvanceOrigin = backgroundOrigin;
                    return false;
                }

                // 玩家手动推进或快进时不应遗留旧的自动推进请求。
                session.PendingAdvanceOrigin = TalkAdvanceOrigin.None;
                return true;
            }

            if (!IsOwnedSingleTalkSession(session, owner))
            {
                return true;
            }

            DisableFastForwardNow(owner);
            TalkAdvanceOrigin origin = GetAdvanceOrigin(owner);

            if (Time.unscaledTime < session.MinimumAdvanceAt)
            {
                if (origin == TalkAdvanceOrigin.System && session.PlayMode == 2)
                {
                    ScheduleAdvance(owner, origin, session.MinimumAdvanceAt);
                }
                else if (origin != TalkAdvanceOrigin.Manual && session.PlayMode != 2)
                {
                    session.PendingAdvanceOrigin = origin;
                }
                return false;
            }

            switch (session.PlayMode)
            {
                case 1:
                    if (origin == TalkAdvanceOrigin.Manual)
                    {
                        EndActiveSessionInternal("类型1手动进入 nexttalk", true, true, false);
                        return true;
                    }

                    session.PendingAdvanceOrigin = origin;
                    return false;

                case 2:
                    if (origin == TalkAdvanceOrigin.Manual || origin == TalkAdvanceOrigin.System)
                    {
                        EndActiveSessionInternal("类型2进入 nexttalk", true, true, false);
                        return true;
                    }

                    return false;

                case 3:
                    if (origin != TalkAdvanceOrigin.Manual)
                    {
                        session.PendingAdvanceOrigin = origin;
                    }
                    return false;

                default:
                    return true;
            }
        }

        internal bool ShouldBlockFastForward(BaseView owner)
        {
            return IsOwnedSingleTalkSession(_session, owner);
        }

        internal bool ShouldBlockClose(BaseView owner)
        {
            return IsOwnedSingleTalkSession(_session, owner) && _session.PlayMode == 3;
        }

        internal void BeforeTalkRefresh(BaseView owner, int newTalkId)
        {
            BetterMusicSession session = _session;
            if (IsOwnedSingleTalkSession(session, owner) && session.TalkId != newTalkId)
            {
                EndActiveSessionInternal("进入新的 Talk", true, true, false);
            }
        }

        internal void AfterTalkRefresh(BaseView owner, int newTalkId)
        {
            BetterMusicSession session = _session;
            if (session == null || session.IsCancelled ||
                session.Scope != BetterMusicPlaybackScope.Background)
            {
                return;
            }

            session.OwnerView = owner;
            session.TalkId = newTalkId;
            AttachLyricsIfPossible(session, owner);
        }

        internal void CleanupForView(BaseView owner, string reason)
        {
            BetterMusicSession session = _session;
            if (session == null)
            {
                ClearHoldState(owner);
                return;
            }

            if (IsOwnedSingleTalkSession(session, owner))
            {
                EndActiveSessionInternal(reason, true, true, false);
            }
            else if (session.Scope == BetterMusicPlaybackScope.Background &&
                     ReferenceEquals(session.OwnerView, owner))
            {
                session.OwnerView = null;
                _lyricsOverlay?.Destroy();
            }

            ClearHoldState(owner);
        }

        internal bool HandleHoldStart(BaseView owner, int hotKey)
        {
            if (hotKey != HoldToSkipHotKey || !IsLockedModeThree(owner))
            {
                return false;
            }

            if (!(owner is NewTalkUI talkUi))
            {
                return false;
            }

            _holdOwner = owner;
            _holdProgress = 0f;
            _holdToSkipOverlay?.Show(talkUi);
            return true;
        }

        internal bool HandleHoldTick(BaseView owner, int hotKey)
        {
            if (hotKey != HoldToSkipHotKey || !ReferenceEquals(_holdOwner, owner) || !IsLockedModeThree(owner))
            {
                return false;
            }

            _holdProgress = Mathf.Clamp01(
                _holdProgress + Time.unscaledDeltaTime / HoldToSkipSeconds);
            _holdToSkipOverlay?.SetProgress(_holdProgress);

            if (_holdProgress < 1f)
            {
                return true;
            }

            _holdToSkipOverlay?.Hide();
            _holdOwner = null;
            _holdProgress = 0f;

            EndActiveSessionInternal("长按鼠标跳过类型3音乐", true, true, false);
            InvokeNextTalk(owner);
            return true;
        }

        internal bool HandleHoldRelease(BaseView owner, int hotKey)
        {
            if (hotKey != HoldToSkipHotKey ||
                (!ReferenceEquals(_holdOwner, owner) && !IsLockedModeThree(owner)))
            {
                return false;
            }

            ClearHoldState(owner);
            return true;
        }

        private bool IsLockedModeThree(BaseView owner)
        {
            return IsOwnedSingleTalkSession(_session, owner) && _session.PlayMode == 3;
        }

        private void ClearHoldState(BaseView owner)
        {
            if (_holdOwner != null && owner != null && !ReferenceEquals(_holdOwner, owner))
            {
                return;
            }

            _holdOwner = null;
            _holdProgress = 0f;
            _holdToSkipOverlay?.Hide();
        }

        private void CompleteOneShot(BetterMusicSession session)
        {
            if (_session == null || !ReferenceEquals(_session, session) || session.IsCancelled)
            {
                return;
            }

            BaseView owner = session.OwnerView;
            TalkAdvanceOrigin pending = session.PendingAdvanceOrigin;
            float minimumAdvanceAt = session.MinimumAdvanceAt;

            EndActiveSessionInternal("单次播放自然完成", true, true, false);

            if (pending != TalkAdvanceOrigin.None && IsOwnerAlive(owner))
            {
                ContinueAdvance(owner, pending, minimumAdvanceAt);
            }
        }

        private void FailActiveSession(string reason)
        {
            BetterMusicSession session = _session;
            BaseView owner = session?.OwnerView;
            TalkAdvanceOrigin pending = session?.PendingAdvanceOrigin ?? TalkAdvanceOrigin.None;
            float minimumAdvanceAt = session?.MinimumAdvanceAt ?? 0f;

            Plugin.LogEffectError(reason);
            EndActiveSessionInternal(reason, true, true, false);

            if (pending != TalkAdvanceOrigin.None && IsOwnerAlive(owner))
            {
                ContinueAdvance(owner, pending, minimumAdvanceAt);
            }
        }

        private void ContinueAdvance(
            BaseView owner,
            TalkAdvanceOrigin origin,
            float minimumAdvanceAt)
        {
            if (Time.unscaledTime < minimumAdvanceAt)
            {
                ScheduleAdvance(owner, origin, minimumAdvanceAt);
                return;
            }

            InvokeNextTalk(owner);
        }

        private void ScheduleAdvance(
            BaseView owner,
            TalkAdvanceOrigin origin,
            float executeAt)
        {
            _scheduledAdvanceOwner = owner;
            _scheduledAdvanceOrigin = origin;
            _scheduledAdvanceAt = Mathf.Max(Time.unscaledTime, executeAt);
        }

        private void ProcessScheduledAdvance()
        {
            BaseView owner = _scheduledAdvanceOwner;
            if (owner == null)
            {
                return;
            }

            if (!IsOwnerAlive(owner))
            {
                ClearScheduledAdvance();
                return;
            }

            if (Time.unscaledTime < _scheduledAdvanceAt)
            {
                return;
            }

            TalkAdvanceOrigin origin = _scheduledAdvanceOrigin;
            ClearScheduledAdvance();

            BeginAdvance(owner, origin);
            try
            {
                InvokeNextTalk(owner);
            }
            finally
            {
                EndAdvance(owner);
            }
        }

        private void ClearScheduledAdvance()
        {
            _scheduledAdvanceOwner = null;
            _scheduledAdvanceOrigin = TalkAdvanceOrigin.None;
            _scheduledAdvanceAt = 0f;
        }

        private static void InvokeNextTalk(BaseView owner)
        {
            try
            {
                if (owner is NewTalkView runtimeView)
                {
                    runtimeView.NextTalk();
                }
                else if (owner is PreviewTalkView previewView)
                {
                    previewView.NextTalk();
                }
            }
            catch (Exception ex)
            {
                Plugin.LogEffectError($"音乐播放完成后进入 nexttalk 失败：{ex.Message}");
            }
        }

        private void PauseActiveMusic()
        {
            BetterMusicSession session = _session;
            if (session == null || session.IsCancelled || session.IsLoading ||
                session.IsPaused || !session.IsPlaying)
            {
                Plugin.LogEffectError("1163,99,1 暂停失败：当前没有正在播放的插件音乐。");
                return;
            }

            AudioSource source = GetUsableAudioSource();
            if (source == null || source.clip == null)
            {
                Plugin.LogEffectError("1163,99,1 暂停失败：插件 AudioSource 或音频已失效。");
                return;
            }

            try
            {
                session.PausedAtSeconds = source.time;
                source.Pause();
                session.IsPaused = true;
                session.IsPlaying = false;
                _lyricsOverlay?.Clear();
                ReleaseGameMusicSuppression();
                Plugin.LogEffectSuccess(
                    $"已暂停插件音乐；音乐={session.ResolvedMusic?.Name ?? "未知"}" +
                    $"（ID={session.MusicId}），暂停点={session.PausedAtSeconds:F3}秒。");
            }
            catch (Exception ex)
            {
                Plugin.LogEffectError($"1163,99,1 暂停失败：{ex.Message}");
            }
        }

        private void ResumeActiveMusic()
        {
            BetterMusicSession session = _session;
            if (session == null || session.IsCancelled || !session.IsPaused)
            {
                Plugin.LogEffectError("1163,99,2 恢复失败：当前没有已暂停的插件音乐。");
                return;
            }

            AudioSource source = GetUsableAudioSource();
            if (source == null || source.clip == null)
            {
                Plugin.LogEffectError("1163,99,2 恢复失败：插件 AudioSource 或音频已失效。");
                return;
            }

            try
            {
                AcquireGameMusicSuppression();

                float maxTime = session.HasPlaybackEndBoundary
                    ? Mathf.Max(session.PlaybackStartSeconds, session.PlaybackEndSeconds - 0.01f)
                    : Mathf.Max(0f, source.clip.length - 0.01f);
                float resumeTime = session.PausedAtSeconds;
                if (resumeTime < session.PlaybackStartSeconds || resumeTime >= maxTime)
                {
                    resumeTime = session.PlaybackStartSeconds;
                }
                source.time = Mathf.Clamp(resumeTime, session.PlaybackStartSeconds, maxTime);
                source.loop = session.ShouldLoop && !session.UsesManualSegmentLoop;
                source.Play();

                session.IsPaused = false;
                session.IsPlaying = true;
                ResetLyrics(session);
                MaintainGameMusicSuppression();
                Plugin.LogEffectSuccess(
                    $"已恢复插件音乐；音乐={session.ResolvedMusic?.Name ?? "未知"}" +
                    $"（ID={session.MusicId}），恢复点={source.time:F3}秒。");
            }
            catch (Exception ex)
            {
                ReleaseGameMusicSuppression();
                Plugin.LogEffectError($"1163,99,2 恢复失败：{ex.Message}");
            }
        }

        private void StopAndRefreshChannel(string reason)
        {
            if (_session != null)
            {
                _session.IsCancelled = true;
            }
            _session = null;
            ++_tokenCounter;

            StopAndClearPluginSource();
            _lyricsOverlay?.Destroy();
            _holdToSkipOverlay?.Destroy();
            _holdToSkipOverlay = new HoldToSkipOverlay();
            _holdOwner = null;
            _holdProgress = 0f;
            ClearScheduledAdvance();
            ReleaseGameMusicSuppression();
            InvalidateAudioSource(reason);

            Plugin.LogEffectSuccess("已停止并刷新插件音乐播放通道。");
        }

        private BetterMusicSession EndActiveSessionInternal(
            string reason,
            bool clearLyrics,
            bool restoreGameMusic,
            bool resetPluginChannel)
        {
            BetterMusicSession oldSession = _session;
            if (oldSession != null)
            {
                oldSession.IsCancelled = true;
                _session = null;
                ++_tokenCounter;
            }

            StopAndClearPluginSource();

            if (clearLyrics)
            {
                _lyricsOverlay?.Destroy();
            }

            ClearScheduledAdvance();
            ClearHoldState(oldSession?.OwnerView);

            if (restoreGameMusic)
            {
                ReleaseGameMusicSuppression();
            }

            if (resetPluginChannel)
            {
                InvalidateAudioSource(reason);
            }

            return oldSession;
        }

        public void Shutdown()
        {
            EndActiveSessionInternal("Shutdown", true, true, false);
            ClearScheduledAdvance();
            _holdToSkipOverlay?.Destroy();
            _lyricsOverlay?.Destroy();
            InvalidateAudioSource("Shutdown");

            if (ReferenceEquals(Instance, this))
            {
                Instance = null;
            }
        }

        private static bool HasReachedPlaybackEnd(
            BetterMusicSession session,
            AudioSource source)
        {
            return session.HasPlaybackEndBoundary &&
                   source.time >= session.PlaybackEndSeconds - 0.01f;
        }

        private void RestartLoop(BetterMusicSession session, AudioSource source)
        {
            if (_session == null || !ReferenceEquals(_session, session) || session.IsPaused)
            {
                return;
            }

            ResetLyrics(session);
            source.loop = session.ShouldLoop && !session.UsesManualSegmentLoop;
            source.time = Mathf.Clamp(
                session.PlaybackStartSeconds,
                0f,
                Mathf.Max(0f, source.clip.length - 0.01f));
            source.Play();
        }

        private void UpdateLyrics(BetterMusicSession session, AudioSource source)
        {
            if (!session.ShowLyrics || session.Lyrics == null || session.Lyrics.Count == 0)
            {
                return;
            }

            float currentTime = source.time;
            if (session.LastPlaybackTime >= 0f && currentTime + 0.05f < session.LastPlaybackTime)
            {
                ResetLyrics(session);
            }
            session.LastPlaybackTime = currentTime;

            int nextIndex = session.LyricIndex;
            while (nextIndex + 1 < session.Lyrics.Count &&
                   session.Lyrics[nextIndex + 1].TimeSeconds <= currentTime + 0.01f)
            {
                nextIndex++;
            }

            if (nextIndex == session.LyricIndex)
            {
                return;
            }

            session.LyricIndex = nextIndex;
            if (nextIndex < 0)
            {
                _lyricsOverlay?.Clear();
                return;
            }

            LrcLine line = session.Lyrics[nextIndex];
            if (!session.IsSinging)
            {
                // 背景型音乐会正确识别 idN 和双语 LRC，但忽略演唱角色语义。
                _lyricsOverlay?.ShowLine(line, null, -1);
                return;
            }

            SingingRoleInfo singer;
            if (line.SingerSlot <= 0 ||
                session.SingingRoles == null ||
                !session.SingingRoles.TryGetValue(line.SingerSlot, out singer))
            {
                singer = SingingRoleResolver.Choir(line.SingerSlot);
            }

            _lyricsOverlay?.ShowLine(
                line,
                singer.Name,
                singer.InternalColorMode);
        }

        private void ResetLyrics(BetterMusicSession session)
        {
            session.LyricIndex = -1;
            session.LastPlaybackTime = -1f;
            _lyricsOverlay?.Clear();
        }

        private void AttachLyricsIfPossible(BetterMusicSession session, BaseView owner)
        {
            if (session == null || !session.ShowLyrics ||
                session.Lyrics == null || session.Lyrics.Count == 0)
            {
                _lyricsOverlay?.Destroy();
                return;
            }

            if (owner is NewTalkUI talkUi)
            {
                _lyricsOverlay?.Attach(
                    talkUi,
                    session.LyricSizeMode,
                    session.LyricColorMode);

                // 背景音乐跨 Talk 重建歌词控件后，需要按当前播放时间重新定位歌词。
                session.LyricIndex = -1;
                session.LastPlaybackTime = -1f;
            }
            else
            {
                _lyricsOverlay?.Destroy();
            }
        }

        private void DisableFastForwardNow(BaseView owner)
        {
            if (!(owner is NewTalkView runtimeView))
            {
                return;
            }

            try
            {
                runtimeView.SpeedUp(false);
            }
            catch
            {
                Time.timeScale = 1f;
            }
        }

        private void AcquireGameMusicSuppression()
        {
            if (!_gameMusicSuppressionHeld)
            {
                _gameMusicSuppressionHeld = true;
                _resumeGameMusicWhenReleased = false;
                _suppressedGameMusicSource = null;
                _gameMusicWarningLogged = false;
            }

            MaintainGameMusicSuppression();
        }

        private void MaintainGameMusicSuppression()
        {
            if (!_gameMusicSuppressionHeld)
            {
                return;
            }

            try
            {
                AudioSource gameMusicSource = TryGetGameMusicSource();
                if (gameMusicSource == null || !gameMusicSource.isPlaying)
                {
                    return;
                }

                AudioMgrEx.PauseMusic();
                _resumeGameMusicWhenReleased = true;
                _suppressedGameMusicSource = gameMusicSource;

            }
            catch (Exception ex)
            {
                if (!_gameMusicWarningLogged)
                {
                    _gameMusicWarningLogged = true;
                    Plugin.LogEffectError(
                        $"插件音乐无法取得原版音乐通道优先级：{ex.Message}");
                }
            }
        }

        private void ReleaseGameMusicSuppression()
        {
            if (!_gameMusicSuppressionHeld && !_resumeGameMusicWhenReleased)
            {
                return;
            }

            bool shouldResume = _resumeGameMusicWhenReleased;
            AudioSource suppressedSource = _suppressedGameMusicSource;

            // 先清状态，防止 UnPause 过程中触发的游戏逻辑再次被误判为仍受插件压制。
            _gameMusicSuppressionHeld = false;
            _resumeGameMusicWhenReleased = false;
            _suppressedGameMusicSource = null;
            _gameMusicWarningLogged = false;

            if (!shouldResume)
            {
                return;
            }

            try
            {
                AudioMgrEx.UnPauseMusic();
            }
            catch (Exception ex)
            {
                // AudioMgr 包装层异常时，尝试直接归还当初被插件暂停的独立原版源。
                try
                {
                    if (suppressedSource != null)
                    {
                        suppressedSource.UnPause();
                        return;
                    }
                }
                catch (Exception fallbackEx)
                {
                    Plugin.LogEffectError(
                        $"原版音乐通道恢复失败：{ex.Message}；后备恢复同样失败：{fallbackEx.Message}");
                    return;
                }

                Plugin.LogEffectError($"原版音乐通道恢复失败：{ex.Message}");
            }
        }

        private static AudioSource TryGetGameMusicSource()
        {
            try
            {
                AudioMgr audioMgr = AudioMgr.Ins;
                Channel channel = audioMgr?.GetChannel(1);
                return channel?.source;
            }
            catch
            {
                return null;
            }
        }

        private AudioSource EnsureAudioSource(string reason, bool forceRecreate = false)
        {
            if (!forceRecreate)
            {
                AudioSource existing = GetUsableAudioSource();
                if (existing != null)
                {
                    return existing;
                }
            }

            if (forceRecreate || _audioSource != null || _audioObject != null)
            {
                InvalidateAudioSource(reason);
            }

            try
            {
                _audioObject = new GameObject("lf-BetterMusicAudioSource");
                DontDestroyOnLoad(_audioObject);
                _audioSource = _audioObject.AddComponent<AudioSource>();
                _audioSource.playOnAwake = false;
                _audioSource.loop = false;
                _audioSource.spatialBlend = 0f;
                _audioSource.volume = 1f;
                return _audioSource;
            }
            catch
            {
                _audioSource = null;
                _audioObject = null;
                return null;
            }
        }

        private AudioSource GetUsableAudioSource()
        {
            try
            {
                return _audioSource != null && _audioSource.gameObject != null
                    ? _audioSource
                    : null;
            }
            catch
            {
                return null;
            }
        }

        private void StopAndClearPluginSource()
        {
            AudioSource source = GetUsableAudioSource();
            if (source == null)
            {
                return;
            }

            try
            {
                source.Stop();
                source.loop = false;
                source.clip = null;
            }
            catch
            {
                InvalidateAudioSource("清理失败");
            }
        }

        private void InvalidateAudioSource(string reason)
        {
            AudioSource source = _audioSource;
            GameObject audioObject = _audioObject;
            _audioSource = null;
            _audioObject = null;

            try
            {
                if (audioObject != null)
                {
                    Destroy(audioObject);
                }
                else if (source != null)
                {
                    Destroy(source);
                }
            }
            catch
            {
                // Unity native object 已失效时直接丢弃引用。
            }
        }

        private void TryBindGameMusicMixer(AudioSource source)
        {
            if (source == null)
            {
                return;
            }

            try
            {
                AudioMgr audioMgr = AudioMgr.Ins;
                Channel channel = audioMgr?.GetChannel(1);
                AudioSource gameSource = channel?.source;
                if (gameSource != null)
                {
                    source.outputAudioMixerGroup = gameSource.outputAudioMixerGroup;
                }
            }
            catch
            {
                // Mixer 绑定失败不影响独立 AudioSource 播放。
            }
        }

        private static bool IsOwnedSingleTalkSession(
            BetterMusicSession session,
            BaseView owner)
        {
            return session != null && !session.IsCancelled &&
                   session.Scope == BetterMusicPlaybackScope.SingleTalk &&
                   ReferenceEquals(session.OwnerView, owner);
        }

        private static bool IsOwnerAlive(BaseView owner)
        {
            return owner != null && owner.gameObject != null && owner.gameObject.activeInHierarchy;
        }

        private static bool IsControllerUsable(BetterMusicController controller)
        {
            if (controller == null)
            {
                return false;
            }

            try
            {
                return controller.gameObject != null;
            }
            catch
            {
                return false;
            }
        }

        private void OnApplicationQuit()
        {
            _applicationQuitting = true;
            Plugin.Instance?.NotifyApplicationQuitFromPersistentController();
            Shutdown();
        }

        private void OnDestroy()
        {
            try
            {
                if (_session != null || _gameMusicSuppressionHeld ||
                    _audioSource != null || _audioObject != null)
                {
                    Shutdown();
                }
            }
            catch
            {
                // Unity 正在退出或销毁 native object 时只做 best-effort 清理。
            }

            if (ReferenceEquals(Instance, this))
            {
                Instance = null;
            }
        }
    }
}
