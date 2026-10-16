using UnityEngine;

namespace Darkest.Unity.Presentation.Ui
{
    /// <summary>UI 색/크기 상수. 더미 UI라도 숫자를 흩뿌리지 않고 여기서만 바꾼다.</summary>
    public static class UiTheme
    {
        public static readonly Vector2 ReferenceResolution = new Vector2(1920, 1080);
        public const float MatchWidthOrHeight = 0.5f;

        public static readonly Color Background = new Color(0.08f, 0.07f, 0.07f, 0.92f);
        public static readonly Color Panel = new Color(0.15f, 0.13f, 0.12f, 0.95f);
        public static readonly Color Button = new Color(0.32f, 0.24f, 0.18f, 1f);
        public static readonly Color ButtonHighlighted = new Color(0.55f, 0.38f, 0.22f, 1f);
        public static readonly Color ButtonSelected = new Color(0.62f, 0.2f, 0.16f, 1f);
        public static readonly Color ButtonDisabled = new Color(0.2f, 0.2f, 0.2f, 0.6f);
        public static readonly Color Text = new Color(0.92f, 0.88f, 0.8f, 1f);
        public static readonly Color TextDim = new Color(0.65f, 0.6f, 0.55f, 1f);

        public const int TitleFontSize = 34;
        public const int BodyFontSize = 24;
        public const int SmallFontSize = 20;
        public const float ButtonHeight = 52f;
        public const float Spacing = 8f;
        public const int Padding = 12;
        public const int MaxLogLines = 7;

        /// <summary>화면 아래 공용 로그 패널이 차지하는 높이 비율. 각 화면은 그 위 영역을 쓴다.</summary>
        public const float LogPanelHeight = 0.2f;
    }
}
