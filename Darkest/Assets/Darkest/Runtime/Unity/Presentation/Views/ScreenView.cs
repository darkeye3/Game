using Darkest.Unity.Presentation.Ui;
using UnityEngine;
using UnityEngine.UI;

namespace Darkest.Unity.Presentation.Views
{
    /// <summary>
    /// 화면 View 베이스. View는 "그리기 + 입력 이벤트 발행"만 한다. 게임 규칙은 모르며 Presenter가 문자열/목록을 넘겨준다.
    /// </summary>
    public abstract class ScreenView : MonoBehaviour
    {
        protected RectTransform Root { get; private set; }
        protected UiFactory Factory { get; private set; }

        public bool IsVisible => Root != null && Root.gameObject.activeSelf;

        public void Build(UiFactory factory, RectTransform parent)
        {
            Factory = factory;
            Root = factory.CreatePanel(parent, GetType().Name, UiTheme.Background, new Vector2(0f, UiTheme.LogPanelHeight), Vector2.one);
            BuildContent(Root);
            Hide();
        }

        public void Show() => Root.gameObject.SetActive(true);
        public void Hide() => Root.gameObject.SetActive(false);

        protected abstract void BuildContent(RectTransform root);

        /// <summary>화면 일부 영역을 차지하는 세로 패널(anchor 비율로 배치).</summary>
        protected RectTransform Column(RectTransform root, string name, float xMin, float xMax, float yMin, float yMax)
        {
            var panel = Factory.CreatePanel(root, name, UiTheme.Panel, new Vector2(xMin, yMin), new Vector2(xMax, yMax));
            return Factory.CreateVertical(panel, name + "Layout");
        }

        protected Button AddButton(Transform parent, string label, System.Action onClick)
        {
            var button = Factory.CreateButton(parent, label, out _);
            button.GetComponentInChildren<Text>().text = label;
            button.onClick.AddListener(() => onClick?.Invoke());
            return button;
        }
    }
}
