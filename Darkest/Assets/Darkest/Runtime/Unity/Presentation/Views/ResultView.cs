using System;
using Darkest.Unity.Presentation.Ui;
using UnityEngine;
using UnityEngine.UI;

namespace Darkest.Unity.Presentation.Views
{
    public sealed class ResultView : ScreenView
    {
        private Text _summary;

        public event Action ReturnClicked;

        protected override void BuildContent(RectTransform root)
        {
            var center = Column(root, "Result", 0.3f, 0.7f, 0.2f, 0.8f);
            _summary = Factory.CreateText(center, "Summary", UiTheme.BodyFontSize, TextAnchor.MiddleCenter);
            AddButton(center, "마을로 귀환", () => ReturnClicked?.Invoke());
        }

        public void SetSummary(string text) => _summary.text = text;
    }
}
