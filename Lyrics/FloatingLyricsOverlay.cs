using GenUI.Talk;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LFBetterMusic.Lyrics
{
    public sealed class FloatingLyricsOverlay
    {
        private const float TargetY = -86f;
        private const float StartOffsetY = -14f;
        private const float FadeSpeed = 7.5f;
        private const float MoveSpeed = 9f;
        private const float BaseFontSize = 38f;
        private const float MaximumContentWidth = 1840f;
        private const float MaximumPrefixWidth = 620f;
        private const float DarkToLightOutlineThreshold = 0.40f;
        private const float LightToDarkOutlineThreshold = 0.60f;

        private static readonly Color DarkOutlineColor = new Color(0f, 0f, 0f, 0.90f);
        private static readonly Color LightOutlineColor = new Color(1f, 1f, 1f, 0.84f);

        private GameObject _root;
        private RectTransform _rect;
        private CanvasGroup _canvasGroup;
        private TextMeshProUGUI _prefixText;
        private TextMeshProUGUI _lyricText;
        private RectTransform _prefixRect;
        private RectTransform _lyricRect;
        private LyricsContrastProbe _contrastProbe;
        private string _currentKey;
        private float _fontSize = BaseFontSize;
        private float _fontScale = 1f;
        private Color _baseColor = Color.white;
        private bool _usingDarkOutline = true;

        public bool IsAlive => _root != null;
        internal bool CanSampleContrast =>
            IsAlive &&
            _canvasGroup != null &&
            _canvasGroup.alpha > 0.05f &&
            !string.IsNullOrEmpty(_currentKey);

        public void Attach(NewTalkUI owner, int sizeMode, int colorMode)
        {
            Destroy();
            if (owner == null || owner.group_top == null)
            {
                return;
            }

            _usingDarkOutline = true;

            _root = new GameObject(
                "BetterMusicFloatingLyrics",
                typeof(RectTransform),
                typeof(CanvasGroup),
                typeof(LyricsContrastProbe));
            _root.transform.SetParent(owner.group_top, false);
            _root.transform.SetAsLastSibling();

            _rect = _root.GetComponent<RectTransform>();
            _rect.anchorMin = new Vector2(0.5f, 1f);
            _rect.anchorMax = new Vector2(0.5f, 1f);
            _rect.pivot = new Vector2(0.5f, 1f);
            _rect.sizeDelta = new Vector2(1900f, 230f);
            _rect.anchoredPosition = new Vector2(0f, TargetY + StartOffsetY);

            _canvasGroup = _root.GetComponent<CanvasGroup>();
            _canvasGroup.alpha = 0f;
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;

            _contrastProbe = _root.GetComponent<LyricsContrastProbe>();
            _contrastProbe?.Configure(this, _rect);

            _prefixText = CreateText("SingerPrefix", out _prefixRect);
            _lyricText = CreateText("LyricBlock", out _lyricRect);

            TMP_FontAsset ownerFont = owner.txtex_content != null
                ? owner.txtex_content.font
                : null;
            if (ownerFont != null)
            {
                _prefixText.font = ownerFont;
                _lyricText.font = ownerFont;
            }

            ApplyStyle(sizeMode, colorMode);
            Clear();
        }

        public void ApplyStyle(int sizeMode, int colorMode)
        {
            float scale;
            switch (sizeMode)
            {
                case 2:
                    scale = 1.2f;
                    break;
                case 3:
                    scale = 1.5f;
                    break;
                case 4:
                    scale = 1.8f;
                    break;
                default:
                    scale = 1f;
                    break;
            }

            _fontScale = scale;
            _fontSize = BaseFontSize * scale;
            _baseColor = ResolveColor(colorMode);
            _baseColor.a = 0.98f;

            ApplyTextStyle(_prefixText, _baseColor);
            ApplyTextStyle(_lyricText, _baseColor);
            ApplyOutlineAndShadow();
        }

        public void ShowLine(
            LrcLine line,
            string singerName,
            int colorMode)
        {
            if (line == null)
            {
                Clear();
                return;
            }

            ShowLine(
                line.PrimaryText,
                line.SecondaryText,
                singerName,
                colorMode);
        }

        public void ShowLine(
            string primaryText,
            string secondaryText,
            string singerName,
            int colorMode)
        {
            if (!IsAlive || _prefixText == null || _lyricText == null)
            {
                return;
            }

            primaryText = primaryText ?? string.Empty;
            secondaryText = secondaryText ?? string.Empty;
            singerName = singerName ?? string.Empty;
            string prefix = string.IsNullOrWhiteSpace(singerName)
                ? string.Empty
                : singerName.Trim() + "：";

            string key = prefix + "\u001f" + primaryText + "\u001f" + secondaryText + "\u001f" + colorMode;
            if (key == _currentKey)
            {
                return;
            }

            _currentKey = key;
            _prefixText.text = prefix;
            _lyricText.text = string.IsNullOrEmpty(secondaryText)
                ? primaryText
                : primaryText + "\n" + secondaryText;

            Color color = colorMode < 0 ? _baseColor : ResolveColor(colorMode);
            color.a = 0.98f;
            ApplyTextStyle(_prefixText, color);
            ApplyTextStyle(_lyricText, color);
            RefreshLayout(prefix, _lyricText.text, !string.IsNullOrEmpty(secondaryText));

            _canvasGroup.alpha = 0f;
            _rect.anchoredPosition = new Vector2(0f, TargetY + StartOffsetY);
        }

        public void ShowLine(string text)
        {
            ShowLine(text, string.Empty, string.Empty, -1);
        }

        public void Clear()
        {
            _currentKey = string.Empty;
            if (_prefixText != null)
            {
                _prefixText.text = string.Empty;
            }
            if (_lyricText != null)
            {
                _lyricText.text = string.Empty;
            }
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
            }
        }

        public void Tick(float unscaledDeltaTime)
        {
            if (!IsAlive || _canvasGroup == null || _rect == null ||
                string.IsNullOrEmpty(_currentKey))
            {
                return;
            }

            _canvasGroup.alpha = Mathf.MoveTowards(
                _canvasGroup.alpha,
                1f,
                FadeSpeed * unscaledDeltaTime);
            Vector2 position = _rect.anchoredPosition;
            position.y = Mathf.Lerp(
                position.y,
                TargetY,
                1f - Mathf.Exp(-MoveSpeed * unscaledDeltaTime));
            _rect.anchoredPosition = position;
        }

        public void Destroy()
        {
            if (_root != null)
            {
                Object.Destroy(_root);
            }

            _root = null;
            _rect = null;
            _canvasGroup = null;
            _prefixText = null;
            _lyricText = null;
            _prefixRect = null;
            _lyricRect = null;
            _contrastProbe = null;
            _currentKey = null;
        }

        internal void ApplyBackgroundLuminance(float luminance)
        {
            bool nextDarkOutline = _usingDarkOutline;
            if (_usingDarkOutline && luminance < DarkToLightOutlineThreshold)
            {
                nextDarkOutline = false;
            }
            else if (!_usingDarkOutline && luminance > LightToDarkOutlineThreshold)
            {
                nextDarkOutline = true;
            }

            if (nextDarkOutline == _usingDarkOutline)
            {
                return;
            }

            _usingDarkOutline = nextDarkOutline;
            ApplyOutlineAndShadow();
        }

        private void ApplyOutlineAndShadow()
        {
            Color outlineColor = _usingDarkOutline
                ? DarkOutlineColor
                : LightOutlineColor;

            ApplyEffects(_prefixText, outlineColor);
            ApplyEffects(_lyricText, outlineColor);
        }

        private void ApplyEffects(TextMeshProUGUI text, Color outlineColor)
        {
            if (text == null)
            {
                return;
            }

            text.outlineColor = outlineColor;
            text.outlineWidth = 0.22f;

            Shadow shadow = text.GetComponent<Shadow>();
            if (shadow != null)
            {
                shadow.effectColor = new Color(0f, 0f, 0f, 0.68f);
                shadow.effectDistance = new Vector2(
                    2.6f * _fontScale,
                    -2.6f * _fontScale);
                shadow.useGraphicAlpha = true;
            }
        }

        private TextMeshProUGUI CreateText(string objectName, out RectTransform rect)
        {
            GameObject textObject = new GameObject(
                objectName,
                typeof(RectTransform),
                typeof(TextMeshProUGUI),
                typeof(Shadow));
            textObject.transform.SetParent(_root.transform, false);

            rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0f, 1f);

            TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
            text.raycastTarget = false;
            text.alignment = TextAlignmentOptions.TopLeft;
            text.enableWordWrapping = false;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.fontStyle = FontStyles.Bold;
            text.outlineColor = DarkOutlineColor;
            text.outlineWidth = 0.22f;
            text.text = string.Empty;

            Shadow shadow = textObject.GetComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.72f);
            shadow.effectDistance = new Vector2(3f, -3f);
            shadow.useGraphicAlpha = true;
            return text;
        }

        private void ApplyTextStyle(TextMeshProUGUI text, Color color)
        {
            if (text == null)
            {
                return;
            }

            text.fontSize = _fontSize;
            text.color = color;
        }

        private void RefreshLayout(
            string prefix,
            string lyricBlock,
            bool hasSecondLine)
        {
            if (_prefixText == null || _lyricText == null ||
                _prefixRect == null || _lyricRect == null)
            {
                return;
            }

            float height = _fontSize * (hasSecondLine ? 2.55f : 1.45f);
            float prefixWidth = string.IsNullOrEmpty(prefix)
                ? 0f
                : Mathf.Min(
                    MaximumPrefixWidth,
                    _prefixText.GetPreferredValues(prefix).x + 8f);
            float lyricWidth = Mathf.Min(
                MaximumContentWidth - prefixWidth,
                _lyricText.GetPreferredValues(lyricBlock).x + 12f);
            lyricWidth = Mathf.Max(40f, lyricWidth);

            float totalWidth = Mathf.Min(
                MaximumContentWidth,
                prefixWidth + lyricWidth);
            float startX = -totalWidth * 0.5f;

            _prefixRect.sizeDelta = new Vector2(prefixWidth, height);
            _prefixRect.anchoredPosition = new Vector2(startX, 0f);
            _prefixText.gameObject.SetActive(prefixWidth > 0.01f);

            _lyricRect.sizeDelta = new Vector2(
                Mathf.Max(40f, totalWidth - prefixWidth),
                height);
            _lyricRect.anchoredPosition = new Vector2(startX + prefixWidth, 0f);
        }

        private static Color ResolveColor(int colorMode)
        {
            // 13~15 是唱歌模式内部颜色，不属于作者可输入的 0~12。
            switch (colorMode)
            {
                case 13:
                    return new Color(0.34f, 0.78f, 1f, 1f); // 男：天蓝色
                case 14:
                    return new Color(1f, 0.48f, 0.72f, 1f); // 女：粉色
                case 15:
                    return new Color(0.72f, 0.48f, 1f, 1f); // 合唱：紫色
            }

            if (colorMode <= 0 || colorMode > 12)
            {
                return Color.white;
            }

            float[] hues =
            {
                0f,
                15f / 360f,
                30f / 360f,
                45f / 360f,
                60f / 360f,
                90f / 360f,
                120f / 360f,
                180f / 360f,
                240f / 360f,
                270f / 360f,
                300f / 360f,
                330f / 360f
            };

            return Color.HSVToRGB(hues[colorMode - 1], 0.82f, 1f);
        }
    }
}
