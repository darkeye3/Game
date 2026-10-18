using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Darkest.Unity.Presentation.Ui
{
    /// <summary>
    /// 코드로 uGUI 계층을 만드는 Factory. 더미 단계라 프리팹 없이도 씬 하나로 바로 실행되게 한다.
    /// 나중에 프리팹 UI로 바꿀 때 View 클래스의 공개 API(이벤트/Render)는 그대로 두고 이 생성부만 교체하면 된다.
    /// </summary>
    public sealed class UiFactory
    {
        private const string BuiltinFontName = "LegacyRuntime.ttf";

        public UiFactory()
        {
            Font = Resources.GetBuiltinResource<Font>(BuiltinFontName);
            if (Font == null) Debug.LogError("[Darkest][UI] 내장 폰트를 찾을 수 없습니다: " + BuiltinFontName);
        }

        public Font Font { get; }

        public Canvas CreateCanvas(string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = UiTheme.ReferenceResolution;
            scaler.matchWidthOrHeight = UiTheme.MatchWidthOrHeight;
            EnsureEventSystem();
            return canvas;
        }

        public RectTransform CreatePanel(Transform parent, string name, Color color, Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            go.GetComponent<Image>().color = color;
            return rect;
        }

        public RectTransform CreateVertical(Transform parent, string name, bool expandHeight = false)
        {
            var rect = CreateEmpty(parent, name);
            var layout = rect.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = UiTheme.Spacing;
            layout.padding = new RectOffset(UiTheme.Padding, UiTheme.Padding, UiTheme.Padding, UiTheme.Padding);
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = expandHeight;
            return rect;
        }

        public RectTransform CreateHorizontal(Transform parent, string name, float height = UiTheme.ButtonHeight)
        {
            var rect = CreateEmpty(parent, name);
            var layout = rect.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = UiTheme.Spacing;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;
            SetPreferredHeight(rect, height);
            return rect;
        }

        /// <summary>
        /// 세로 스크롤 목록. 부모 레이아웃에서 남는 높이를 차지하고, 항목이 넘치면 잘리는 대신 스크롤된다.
        /// 반환값은 항목을 넣을 Content(VerticalLayoutGroup + ContentSizeFitter).
        /// </summary>
        public RectTransform CreateScrollList(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(ScrollRect), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = Color.clear; // 휠/드래그 입력을 받기 위한 투명 레이캐스트 영역
            var element = go.GetComponent<LayoutElement>();
            element.minHeight = UiTheme.ButtonHeight;
            element.flexibleHeight = 1f;

            var viewport = CreateEmpty(go.transform, "Viewport");
            viewport.gameObject.AddComponent<RectMask2D>();

            var content = CreateEmpty(viewport, "Content");
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = Vector2.one;
            content.pivot = new Vector2(0.5f, 1f);
            var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = UiTheme.Spacing;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scroll = go.GetComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = UiTheme.ScrollSensitivity;
            return content;
        }

        public Text CreateText(Transform parent, string name, int fontSize, TextAnchor alignment = TextAnchor.UpperLeft, float preferredHeight = -1f)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<Text>();
            text.font = Font;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = UiTheme.Text;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            // 해상도/비율이 바뀌어 공간이 모자라면 잘리는 대신 글자를 줄인다(최대는 원래 크기).
            text.resizeTextForBestFit = true;
            text.resizeTextMaxSize = fontSize;
            text.resizeTextMinSize = Mathf.Max(UiTheme.MinFontSize, Mathf.RoundToInt(fontSize * UiTheme.MinFontScale));
            text.raycastTarget = false;
            if (preferredHeight > 0f) SetPreferredHeight(go.transform, preferredHeight);
            else go.AddComponent<LayoutElement>().flexibleHeight = 1f;
            return text;
        }

        public Button CreateButton(Transform parent, string name, out Text label)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = Color.white;

            var button = go.GetComponent<Button>();
            var colors = button.colors;
            colors.normalColor = UiTheme.Button;
            colors.highlightedColor = UiTheme.ButtonHighlighted;
            colors.pressedColor = UiTheme.ButtonSelected;
            colors.selectedColor = UiTheme.Button;
            colors.disabledColor = UiTheme.ButtonDisabled;
            button.colors = colors;

            label = CreateText(go.transform, "Label", UiTheme.SmallFontSize, TextAnchor.MiddleCenter, UiTheme.ButtonHeight);
            var labelRect = (RectTransform)label.transform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            SetPreferredHeight(go.transform, UiTheme.ButtonHeight);
            return button;
        }

        public static void SetPreferredHeight(Transform target, float height)
        {
            // UnityEngine.Object는 ?? 연산자와 함께 쓰면 '가짜 null'을 놓치므로 명시적으로 비교한다.
            var element = target.GetComponent<LayoutElement>();
            if (element == null) element = target.gameObject.AddComponent<LayoutElement>();
            element.preferredHeight = height;
            element.minHeight = height;
            element.flexibleHeight = 0f;
        }

        private static RectTransform CreateEmpty(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
        }

        /// <summary>
        /// Input System 패키지가 설치돼 있으면(asmdef versionDefines → DARKEST_INPUT_SYSTEM) 새 입력 모듈,
        /// 없으면 구 Input Manager 모듈을 쓴다. 프로젝트 입력 설정과 상관없이 클릭이 동작하게 하기 위함.
        /// </summary>
        private static void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null) return;
#if DARKEST_INPUT_SYSTEM
            new GameObject("EventSystem", typeof(EventSystem), typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
#else
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
#endif
        }
    }
}
