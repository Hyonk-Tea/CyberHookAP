using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CyberHookAP
{
    internal enum PopupKind
    {
        Item,
        Check,
        Trap,
        Info
    }

    internal sealed class EventPopupUi : MonoBehaviour
    {
        private sealed class LogLine
        {
            public PopupKind Kind;
            public string Message;
            public TextMeshProUGUI Label;
            public LayoutElement Layout;
        }

        private static EventPopupUi _instance;

        private readonly List<LogLine> _lines = new List<LogLine>();

        private Canvas _canvas;
        private CanvasGroup _canvasGroup;
        private RectTransform _panelRoot;
        private RectTransform _logContent;
        private ScrollRect _scrollRect;
        private TextMeshProUGUI _titleText;
        private TextMeshProUGUI _statusText;
        private Image _statusAccent;
        private TMP_InputField _inputField;
        private Button _sendButton;
        private TMP_FontAsset _preferredFont;
        private TMP_FontAsset _resolvedFont;
        private float _lastFontRefreshTime;
        private float _lastActivityTime;
        private string _lastStatus = "Disconnected";
        private Action<string> _commandHandler;

        internal static EventPopupUi Instance
        {
            get { return _instance; }
        }

        internal static EventPopupUi Ensure()
        {
            if (_instance != null)
            {
                return _instance;
            }

            GameObject root = new GameObject("CyberHookAP_UI");
            DontDestroyOnLoad(root);
            _instance = root.AddComponent<EventPopupUi>();
            return _instance;
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
            EnsureEventSystem();
            BuildCanvas();
            _lastActivityTime = Time.unscaledTime;
            SetStatus(_lastStatus);
        }

        private void Update()
        {
            RefreshFontIfNeeded();
            UpdateFade();
        }

        internal void SetCommandHandler(Action<string> commandHandler)
        {
            _commandHandler = commandHandler;
        }

        internal void SetStatus(string status)
        {
            string normalized = string.IsNullOrEmpty(status) ? "Disconnected" : status;
            bool changed = !string.Equals(_lastStatus, normalized, StringComparison.OrdinalIgnoreCase);
            _lastStatus = normalized;

            if (_statusText != null)
            {
                _statusText.text = normalized.ToUpperInvariant();
            }

            ApplyStatusVisuals(normalized);
        }

        internal void ShowItem(string message)
        {
        }

        internal void ShowCheck(string message)
        {
        }

        internal void ShowTrap(string message)
        {
        }

        internal void ShowInfo(string title, string message)
        {
            AppendLogLine(PopupKind.Info, message);
        }

        internal void ShowGoal(string message)
        {
        }

        private void BuildCanvas()
        {
            _canvas = gameObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 5000;
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            _canvasGroup.alpha = 1f;

            CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            gameObject.AddComponent<GraphicRaycaster>();

            GameObject panelObj = CreateUiObject("TerminalPanel", gameObject.transform);
            _panelRoot = panelObj.AddComponent<RectTransform>();
            _panelRoot.anchorMin = new Vector2(1f, 1f);
            _panelRoot.anchorMax = new Vector2(1f, 1f);
            _panelRoot.pivot = new Vector2(1f, 1f);
            _panelRoot.anchoredPosition = new Vector2(-28f, -28f);
            _panelRoot.sizeDelta = new Vector2(500f, 250f);

            Image panelBackground = panelObj.AddComponent<Image>();
            panelBackground.color = new Color(0.03f, 0.06f, 0.07f, 0.25f);

            Outline panelOutline = panelObj.AddComponent<Outline>();
            panelOutline.effectColor = new Color(0.16f, 0.30f, 0.33f, 0.85f);
            panelOutline.effectDistance = new Vector2(1f, -1f);

            GameObject headerObj = CreateUiObject("Header", panelObj.transform);
            RectTransform headerRect = headerObj.AddComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0f, 1f);
            headerRect.anchorMax = new Vector2(1f, 1f);
            headerRect.pivot = new Vector2(0.5f, 1f);
            headerRect.anchoredPosition = Vector2.zero;
            headerRect.sizeDelta = new Vector2(0f, 30f);

            Image headerBackground = headerObj.AddComponent<Image>();
            headerBackground.color = new Color(0.05f, 0.09f, 0.10f, 0.25f);

            GameObject accentObj = CreateUiObject("Accent", headerObj.transform);
            RectTransform accentRect = accentObj.AddComponent<RectTransform>();
            accentRect.anchorMin = new Vector2(0f, 0f);
            accentRect.anchorMax = new Vector2(0f, 1f);
            accentRect.pivot = new Vector2(0f, 0.5f);
            accentRect.anchoredPosition = Vector2.zero;
            accentRect.sizeDelta = new Vector2(6f, 0f);
            _statusAccent = accentObj.AddComponent<Image>();

            _titleText = CreateText("Title", headerObj.transform, "ARCHIPELAGO TERMINAL", 17f, FontStyles.Bold);
            RectTransform titleRect = _titleText.rectTransform;
            titleRect.anchorMin = new Vector2(0f, 0f);
            titleRect.anchorMax = new Vector2(0f, 1f);
            titleRect.pivot = new Vector2(0f, 0.5f);
            titleRect.anchoredPosition = new Vector2(20f, 0f);
            titleRect.sizeDelta = new Vector2(300f, 0f);
            _titleText.alignment = TextAlignmentOptions.MidlineLeft;
            _titleText.color = new Color(0.84f, 0.96f, 0.95f, 1f);

            _statusText = CreateText("Status", headerObj.transform, _lastStatus.ToUpperInvariant(), 13f, FontStyles.Bold);
            RectTransform statusRect = _statusText.rectTransform;
            statusRect.anchorMin = new Vector2(1f, 0f);
            statusRect.anchorMax = new Vector2(1f, 1f);
            statusRect.pivot = new Vector2(1f, 0.5f);
            statusRect.anchoredPosition = new Vector2(-44f, 0f);
            statusRect.sizeDelta = new Vector2(160f, 0f);
            _statusText.alignment = TextAlignmentOptions.MidlineRight;

            Button scrollUpButton = CreateButton(headerObj.transform, "ScrollUp", "^", new Vector2(-14f, -8f), new Vector2(24f, 12f));
            scrollUpButton.onClick.AddListener(delegate { ScrollBy(+0.18f); });

            Button scrollDownButton = CreateButton(headerObj.transform, "ScrollDown", "v", new Vector2(-14f, -22f), new Vector2(24f, 12f));
            scrollDownButton.onClick.AddListener(delegate { ScrollBy(-0.18f); });

            GameObject footerObj = CreateUiObject("Footer", panelObj.transform);
            RectTransform footerRect = footerObj.AddComponent<RectTransform>();
            footerRect.anchorMin = new Vector2(0f, 0f);
            footerRect.anchorMax = new Vector2(1f, 0f);
            footerRect.pivot = new Vector2(0.5f, 0f);
            footerRect.anchoredPosition = Vector2.zero;
            footerRect.sizeDelta = new Vector2(0f, 42f);

            Image footerBackground = footerObj.AddComponent<Image>();
            footerBackground.color = new Color(0.04f, 0.08f, 0.09f, 0.25f);

            GameObject inputObj = CreateUiObject("InputField", footerObj.transform);
            RectTransform inputRect = inputObj.AddComponent<RectTransform>();
            inputRect.anchorMin = new Vector2(0f, 0.5f);
            inputRect.anchorMax = new Vector2(1f, 0.5f);
            inputRect.pivot = new Vector2(0f, 0.5f);
            inputRect.anchoredPosition = new Vector2(12f, 0f);
            inputRect.sizeDelta = new Vector2(-106f, 28f);

            Image inputBackground = inputObj.AddComponent<Image>();
            inputBackground.color = new Color(0.02f, 0.05f, 0.06f, 0.35f);

            _inputField = inputObj.AddComponent<TMP_InputField>();
            _inputField.lineType = TMP_InputField.LineType.SingleLine;
            _inputField.characterLimit = 180;
            _inputField.richText = false;
            _inputField.onSubmit.AddListener(SubmitInput);

            GameObject textViewportObj = CreateUiObject("TextViewport", inputObj.transform);
            RectTransform textViewportRect = textViewportObj.AddComponent<RectTransform>();
            textViewportRect.anchorMin = new Vector2(0f, 0f);
            textViewportRect.anchorMax = new Vector2(1f, 1f);
            textViewportRect.offsetMin = new Vector2(10f, 4f);
            textViewportRect.offsetMax = new Vector2(-10f, -4f);
            textViewportObj.AddComponent<RectMask2D>();

            TextMeshProUGUI inputText = CreateText("InputText", textViewportObj.transform, string.Empty, 15f, FontStyles.Normal);
            RectTransform inputTextRect = inputText.rectTransform;
            inputTextRect.anchorMin = new Vector2(0f, 0f);
            inputTextRect.anchorMax = new Vector2(1f, 1f);
            inputTextRect.offsetMin = Vector2.zero;
            inputTextRect.offsetMax = Vector2.zero;
            inputText.alignment = TextAlignmentOptions.MidlineLeft;
            inputText.color = new Color(0.82f, 0.95f, 0.90f, 1f);
            inputText.enableWordWrapping = false;

            TextMeshProUGUI placeholder = CreateText("Placeholder", textViewportObj.transform, "!HELP FOR HELP", 15f, FontStyles.Italic);
            RectTransform placeholderRect = placeholder.rectTransform;
            placeholderRect.anchorMin = new Vector2(0f, 0f);
            placeholderRect.anchorMax = new Vector2(1f, 1f);
            placeholderRect.offsetMin = Vector2.zero;
            placeholderRect.offsetMax = Vector2.zero;
            placeholder.alignment = TextAlignmentOptions.MidlineLeft;
            placeholder.color = new Color(0.45f, 0.63f, 0.62f, 0.86f);
            placeholder.enableWordWrapping = false;

            _inputField.textViewport = textViewportRect;
            _inputField.textComponent = inputText;
            _inputField.placeholder = placeholder;

            _sendButton = CreateButton(footerObj.transform, "SendButton", "SEND", new Vector2(-12f, 0f), new Vector2(82f, 28f));
            _sendButton.onClick.AddListener(delegate { SubmitInput(_inputField != null ? _inputField.text : string.Empty); });

            GameObject scrollObj = CreateUiObject("ScrollView", panelObj.transform);
            RectTransform scrollRect = scrollObj.AddComponent<RectTransform>();
            scrollRect.anchorMin = new Vector2(0f, 0f);
            scrollRect.anchorMax = new Vector2(1f, 1f);
            scrollRect.offsetMin = new Vector2(12f, 48f);
            scrollRect.offsetMax = new Vector2(-12f, -34f);

            GameObject viewportObj = CreateUiObject("Viewport", scrollObj.transform);
            RectTransform viewportRect = viewportObj.AddComponent<RectTransform>();
            viewportRect.anchorMin = new Vector2(0f, 0f);
            viewportRect.anchorMax = new Vector2(1f, 1f);
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;
            Image viewportImage = viewportObj.AddComponent<Image>();
            viewportImage.color = new Color(0.02f, 0.04f, 0.05f, 0.08f);
            Mask viewportMask = viewportObj.AddComponent<Mask>();
            viewportMask.showMaskGraphic = false;

            GameObject contentObj = CreateUiObject("Content", viewportObj.transform);
            _logContent = contentObj.AddComponent<RectTransform>();
            _logContent.anchorMin = new Vector2(0f, 1f);
            _logContent.anchorMax = new Vector2(1f, 1f);
            _logContent.pivot = new Vector2(0.5f, 1f);
            _logContent.anchoredPosition = Vector2.zero;
            _logContent.sizeDelta = new Vector2(0f, 0f);

            VerticalLayoutGroup layout = contentObj.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 4f;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            layout.padding = new RectOffset(0, 0, 0, 0);

            ContentSizeFitter fitter = contentObj.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            _scrollRect = scrollObj.AddComponent<ScrollRect>();
            _scrollRect.viewport = viewportRect;
            _scrollRect.content = _logContent;
            _scrollRect.horizontal = false;
            _scrollRect.vertical = true;
            _scrollRect.movementType = ScrollRect.MovementType.Clamped;
            _scrollRect.scrollSensitivity = 24f;
        }

        private void AppendLogLine(PopupKind kind, string message)
        {
            if (_logContent == null || string.IsNullOrEmpty(message))
            {
                return;
            }

            string normalizedMessage = NormalizeMessage(message);
            if (normalizedMessage.Length == 0)
            {
                return;
            }

            _lastActivityTime = Time.unscaledTime;

            bool stickToBottom = _scrollRect == null || _scrollRect.verticalNormalizedPosition <= 0.001f;

            GameObject lineObj = CreateUiObject("LogLine", _logContent);
            LayoutElement element = lineObj.AddComponent<LayoutElement>();
            element.minHeight = 24f;
            element.preferredHeight = 24f;

            TextMeshProUGUI label = CreateText("Label", lineObj.transform, string.Empty, 15f, FontStyles.Normal);
            RectTransform labelRect = label.rectTransform;
            labelRect.anchorMin = new Vector2(0f, 0f);
            labelRect.anchorMax = new Vector2(1f, 1f);
            labelRect.offsetMin = new Vector2(0f, 0f);
            labelRect.offsetMax = new Vector2(0f, 0f);
            label.enableWordWrapping = true;
            label.overflowMode = TextOverflowModes.Overflow;
            label.alignment = TextAlignmentOptions.TopLeft;
            label.color = GetTextColor(kind);
            label.text = FormatLine(normalizedMessage);

            LogLine line = new LogLine();
            line.Kind = kind;
            line.Message = normalizedMessage;
            line.Label = label;
            line.Layout = element;
            _lines.Add(line);

            RefreshLineHeight(line);

            while (_lines.Count > 160)
            {
                LogLine oldest = _lines[0];
                _lines.RemoveAt(0);
                if (oldest != null && oldest.Label != null)
                {
                    Destroy(oldest.Label.transform.parent.gameObject);
                }
            }

            Canvas.ForceUpdateCanvases();
            if (stickToBottom)
            {
                ScrollToBottom();
            }
        }

        private void SubmitInput(string rawText)
        {
            string text = rawText != null ? rawText.Trim() : string.Empty;
            if (text.Length == 0)
            {
                return;
            }

            _lastActivityTime = Time.unscaledTime;

            if (_inputField != null)
            {
                _inputField.text = string.Empty;
                _inputField.ActivateInputField();
            }

            Action<string> handler = _commandHandler;
            if (handler != null)
            {
                handler(text);
            }
        }

        private void ScrollBy(float delta)
        {
            if (_scrollRect == null)
            {
                return;
            }

            _lastActivityTime = Time.unscaledTime;
            _scrollRect.verticalNormalizedPosition = Mathf.Clamp01(_scrollRect.verticalNormalizedPosition + delta);
        }

        private void ScrollToBottom()
        {
            if (_scrollRect == null)
            {
                return;
            }

            _scrollRect.verticalNormalizedPosition = 0f;
        }

        private void ApplyStatusVisuals(string status)
        {
            if (_statusAccent == null)
            {
                return;
            }

            string normalized = status ?? string.Empty;
            if (string.Equals(normalized, "Connected", StringComparison.OrdinalIgnoreCase))
            {
                _statusAccent.color = new Color(0.35f, 0.93f, 0.59f, 1f);
                if (_statusText != null)
                {
                    _statusText.color = new Color(0.86f, 1f, 0.90f, 1f);
                }
            }
            else if (string.Equals(normalized, "Connecting", StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalized, "Handshake", StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalized, "Authenticating", StringComparison.OrdinalIgnoreCase))
            {
                _statusAccent.color = new Color(1f, 0.76f, 0.34f, 1f);
                if (_statusText != null)
                {
                    _statusText.color = new Color(1f, 0.90f, 0.72f, 1f);
                }
            }
            else
            {
                _statusAccent.color = new Color(1f, 0.42f, 0.36f, 1f);
                if (_statusText != null)
                {
                    _statusText.color = new Color(1f, 0.82f, 0.80f, 1f);
                }
            }
        }

        private void RefreshFontIfNeeded()
        {
            if (Time.unscaledTime - _lastFontRefreshTime < 2f)
            {
                return;
            }

            _lastFontRefreshTime = Time.unscaledTime;

            if (_preferredFont == null)
            {
                TryCapturePreferredFont();
            }

            if (_preferredFont != null)
            {
                _resolvedFont = _preferredFont;
            }
            else if (_resolvedFont == null)
            {
                _resolvedFont = TMP_Settings.defaultFontAsset;
            }

            RefreshResolvedFont(_titleText);
            RefreshResolvedFont(_statusText);
            if (_inputField != null)
            {
                RefreshResolvedFont(_inputField.textComponent as TextMeshProUGUI);
                RefreshResolvedFont(_inputField.placeholder as TextMeshProUGUI);
            }

            for (int i = 0; i < _lines.Count; i++)
            {
                RefreshResolvedFont(_lines[i].Label);
                RefreshLineHeight(_lines[i]);
            }
        }

        private void RefreshResolvedFont(TextMeshProUGUI text)
        {
            if (text != null && _resolvedFont != null)
            {
                text.font = _resolvedFont;
            }
        }

        private void TryCapturePreferredFont()
        {
            string sceneName = SceneManager.GetActiveScene().name ?? string.Empty;
            if (sceneName.IndexOf("hub", StringComparison.OrdinalIgnoreCase) < 0)
            {
                return;
            }

            TextMeshProUGUI[] allText = UnityEngine.Object.FindObjectsOfType<TextMeshProUGUI>();
            for (int i = 0; i < allText.Length; i++)
            {
                TextMeshProUGUI candidate = allText[i];
                if (candidate == null || candidate.font == null)
                {
                    continue;
                }

                if (candidate == _titleText || candidate == _statusText)
                {
                    continue;
                }

                _preferredFont = candidate.font;
                _resolvedFont = candidate.font;
                return;
            }
        }

        private void EnsureEventSystem()
        {
            if (UnityEngine.Object.FindObjectOfType<EventSystem>() != null)
            {
                return;
            }

            GameObject eventSystemObj = new GameObject("CyberHookAP_EventSystem");
            DontDestroyOnLoad(eventSystemObj);
            eventSystemObj.layer = 5;
            eventSystemObj.AddComponent<EventSystem>();
            eventSystemObj.AddComponent<StandaloneInputModule>();
        }

        private void UpdateFade()
        {
            if (_canvasGroup == null)
            {
                return;
            }

            bool keepVisible = (_inputField != null && _inputField.isFocused)
                || (_scrollRect != null && _scrollRect.velocity.sqrMagnitude > 0.001f);
            if (keepVisible)
            {
                _lastActivityTime = Time.unscaledTime;
            }

            float idleSeconds = Time.unscaledTime - _lastActivityTime;
            float targetAlpha = idleSeconds < 8f ? 1f : 0.22f;
            _canvasGroup.alpha = Mathf.MoveTowards(_canvasGroup.alpha, targetAlpha, Time.unscaledDeltaTime * 1.6f);
        }

        private void RefreshLineHeight(LogLine line)
        {
            if (line == null || line.Label == null || line.Layout == null)
            {
                return;
            }

            float availableWidth = _logContent != null && _logContent.rect.width > 1f ? _logContent.rect.width : 460f;
            Vector2 preferred = line.Label.GetPreferredValues(line.Label.text, availableWidth, 0f);
            line.Layout.preferredHeight = Mathf.Max(24f, preferred.y);
        }

        private static string NormalizeMessage(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                return string.Empty;
            }

            return message.Replace("\r\n", "\n").Replace('\r', '\n').TrimEnd();
        }

        private static string FormatLine(string message)
        {
            string prefix = "[" + DateTime.Now.ToString("HH:mm:ss") + "] ";
            string continuationPrefix = new string(' ', prefix.Length);
            string[] parts = (message ?? string.Empty).Split('\n');
            for (int i = 0; i < parts.Length; i++)
            {
                parts[i] = (i == 0 ? prefix : continuationPrefix) + parts[i];
            }

            return string.Join("\n", parts);
        }

        private static Color GetTextColor(PopupKind kind)
        {
            switch (kind)
            {
                case PopupKind.Item:
                    return new Color(0.46f, 0.88f, 1f, 1f);
                case PopupKind.Check:
                    return new Color(0.62f, 0.97f, 0.58f, 1f);
                case PopupKind.Trap:
                    return new Color(1f, 0.55f, 0.48f, 1f);
                default:
                    return new Color(0.84f, 0.93f, 0.90f, 1f);
            }
        }

        private static GameObject CreateUiObject(string name, Transform parent)
        {
            GameObject obj = new GameObject(name);
            obj.layer = 5;
            obj.transform.SetParent(parent, false);
            return obj;
        }

        private TextMeshProUGUI CreateText(string name, Transform parent, string text, float size, FontStyles style)
        {
            GameObject obj = CreateUiObject(name, parent);
            TextMeshProUGUI label = obj.AddComponent<TextMeshProUGUI>();
            label.text = text ?? string.Empty;
            label.fontSize = size;
            label.fontStyle = style;
            label.raycastTarget = false;
            label.enableAutoSizing = false;
            label.margin = Vector4.zero;
            label.richText = false;
            if (_resolvedFont == null)
            {
                _resolvedFont = TMP_Settings.defaultFontAsset;
            }

            RefreshResolvedFont(label);
            return label;
        }

        private Button CreateButton(Transform parent, string name, string text, Vector2 anchoredPosition, Vector2 size)
        {
            GameObject buttonObj = CreateUiObject(name, parent);
            RectTransform rect = buttonObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 0.5f);
            rect.anchorMax = new Vector2(1f, 0.5f);
            rect.pivot = new Vector2(1f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            Image image = buttonObj.AddComponent<Image>();
            image.color = new Color(0.07f, 0.14f, 0.15f, 0.92f);

            Button button = buttonObj.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = image.color;
            colors.highlightedColor = new Color(0.10f, 0.20f, 0.22f, 0.96f);
            colors.pressedColor = new Color(0.14f, 0.28f, 0.30f, 1f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(0.10f, 0.10f, 0.10f, 0.4f);
            button.colors = colors;

            TextMeshProUGUI label = CreateText("Label", buttonObj.transform, text, 14f, FontStyles.Bold);
            RectTransform labelRect = label.rectTransform;
            labelRect.anchorMin = new Vector2(0f, 0f);
            labelRect.anchorMax = new Vector2(1f, 1f);
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            label.alignment = TextAlignmentOptions.Midline;
            label.color = new Color(0.82f, 0.95f, 0.92f, 1f);
            label.raycastTarget = false;

            return button;
        }
    }
}
