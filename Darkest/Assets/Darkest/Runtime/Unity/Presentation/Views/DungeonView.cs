using System;
using Darkest.Unity.Presentation.Ui;
using UnityEngine;
using UnityEngine.UI;

namespace Darkest.Unity.Presentation.Views
{
    /// <summary>탐험 화면: 지도(텍스트), 이동 가능한 방, 횃불/골동품/야영/후퇴/저장, 파티 상태, 야영 스킬.</summary>
    public sealed class DungeonView : ScreenView
    {
        private Text _header;
        private Text _map;
        private Text _party;
        private Text _message;
        private PooledButtonList _exits;
        private Button _curioButton;
        private Button _campButton;
        private Text _campTitle;

        public event Action TorchClicked;
        public event Action CurioClicked;
        public event Action RetreatClicked;
        public event Action CampClicked;
        public event Action SaveClicked;

        public PooledButtonList Exits => _exits;
        public PooledButtonList CampSkills { get; private set; }

        protected override void BuildContent(RectTransform root)
        {
            var left = Column(root, "Map", 0.01f, 0.45f, 0.02f, 0.98f);
            _header = Factory.CreateText(left, "Header", UiTheme.BodyFontSize, TextAnchor.MiddleLeft, 70f);
            _map = Factory.CreateText(left, "Map", UiTheme.SmallFontSize);

            var middle = Column(root, "Actions", 0.46f, 0.70f, 0.02f, 0.98f);
            Factory.CreateText(middle, "ExitsTitle", UiTheme.SmallFontSize, TextAnchor.MiddleLeft, 30f).text = "이동";
            _exits = new PooledButtonList(Factory, Factory.CreateScrollList(middle, "Exits"));
            AddButton(middle, "횃불 사용", () => TorchClicked?.Invoke());
            _curioButton = AddButton(middle, "골동품 조사", () => CurioClicked?.Invoke());
            _campButton = AddButton(middle, "야영 (장작 1 + 식량)", () => CampClicked?.Invoke());
            AddButton(middle, "후퇴 (마을로)", () => RetreatClicked?.Invoke());
            AddButton(middle, "저장", () => SaveClicked?.Invoke());
            _message = Factory.CreateText(middle, "Message", UiTheme.SmallFontSize, TextAnchor.MiddleLeft, 80f);

            var right = Column(root, "Party", 0.71f, 0.99f, 0.02f, 0.98f);
            _party = Factory.CreateText(right, "Party", UiTheme.SmallFontSize);
            _campTitle = Factory.CreateText(right, "CampTitle", UiTheme.SmallFontSize, TextAnchor.MiddleLeft, 30f);
            CampSkills = new PooledButtonList(Factory, Factory.CreateScrollList(right, "CampSkills"));
        }

        public void SetCampTitle(string text) => _campTitle.text = text;

        public void SetHeader(string text) => _header.text = text;
        public void SetMap(string text) => _map.text = text;
        public void SetParty(string text) => _party.text = text;
        public void SetMessage(string text) => _message.text = text;
        public void SetCurioAvailable(bool available) => _curioButton.interactable = available;
        public void SetCampAvailable(bool available) => _campButton.interactable = available;
    }
}
