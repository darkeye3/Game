using System.Collections.Generic;
using System.Text;
using Darkest.Unity.Presentation.Ui;
using UnityEngine;
using UnityEngine.UI;

namespace Darkest.Unity.Presentation.Views
{
    /// <summary>모든 화면 공용 로그 패널. 최근 N줄만 유지(링 버퍼).</summary>
    public sealed class EventLogView : MonoBehaviour
    {
        private readonly Queue<string> _lines = new Queue<string>(UiTheme.MaxLogLines + 1);
        private readonly StringBuilder _builder = new StringBuilder();
        private Text _text;

        public void Build(UiFactory factory, RectTransform parent, Vector2 anchorMin, Vector2 anchorMax)
        {
            var panel = factory.CreatePanel(parent, "EventLog", UiTheme.Panel, anchorMin, anchorMax);
            _text = factory.CreateText(factory.CreateVertical(panel, "LogLayout"), "LogText", UiTheme.SmallFontSize, TextAnchor.LowerLeft);
            _text.color = UiTheme.TextDim;
        }

        public void Append(string line)
        {
            if (string.IsNullOrEmpty(line)) return;
            _lines.Enqueue(line);
            while (_lines.Count > UiTheme.MaxLogLines) _lines.Dequeue();

            _builder.Clear();
            foreach (var l in _lines) _builder.AppendLine(l);
            _text.text = _builder.ToString();
        }

        public void Clear()
        {
            _lines.Clear();
            _text.text = string.Empty;
        }
    }
}
