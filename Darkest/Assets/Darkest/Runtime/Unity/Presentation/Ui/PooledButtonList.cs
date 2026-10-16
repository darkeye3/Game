using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Darkest.Unity.Presentation.Ui
{
    /// <summary>
    /// 버튼 목록 Object Pool. Render 때마다 Destroy/Instantiate 하지 않고 기존 버튼을 재사용·숨김 처리한다.
    /// 사용법: Begin() → Add(...) 여러 번 → End()
    /// </summary>
    public sealed class PooledButtonList
    {
        private readonly UiFactory _factory;
        private readonly Transform _container;
        private readonly List<Entry> _entries = new List<Entry>();
        private int _used;

        public PooledButtonList(UiFactory factory, Transform container)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _container = container ? container : throw new ArgumentNullException(nameof(container));
        }

        public void Begin() => _used = 0;

        public void Add(string label, Action onClick, bool interactable = true, bool selected = false)
        {
            if (_used == _entries.Count) _entries.Add(CreateEntry());
            var entry = _entries[_used++];
            entry.Button.gameObject.SetActive(true);
            entry.Label.text = label;
            entry.Button.interactable = interactable;
            entry.Image.color = selected ? UiTheme.ButtonSelected : Color.white;
            entry.Button.onClick.RemoveAllListeners();
            if (onClick != null) entry.Button.onClick.AddListener(() => onClick());
        }

        public void End()
        {
            for (int i = _used; i < _entries.Count; i++) _entries[i].Button.gameObject.SetActive(false);
        }

        private Entry CreateEntry()
        {
            var button = _factory.CreateButton(_container, "PooledButton" + _entries.Count, out var label);
            return new Entry(button, label, button.GetComponent<Image>());
        }

        private readonly struct Entry
        {
            public Entry(Button button, Text label, Image image)
            {
                Button = button;
                Label = label;
                Image = image;
            }

            public Button Button { get; }
            public Text Label { get; }
            public Image Image { get; }
        }
    }
}
