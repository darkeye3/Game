using System;
using Darkest.Unity.Presentation.Ui;
using UnityEngine;
using UnityEngine.UI;

namespace Darkest.Unity.Presentation.Views
{
    /// <summary>마을 화면: 명부(파티 선택), 모집, 선술집, 보급, 출발, 저장.</summary>
    public sealed class TownView : ScreenView
    {
        private Text _header;
        private Text _details;
        private Text _supplies;
        private Text _message;
        private PooledButtonList _roster;
        private PooledButtonList _recruits;

        public event Action TreatClicked;
        public event Action DismissClicked;
        public event Action EmbarkClicked;
        public event Action SaveClicked;
        public event Action NewGameClicked;
        public event Action<int> FoodChanged;
        public event Action<int> TorchChanged;
        public event Action<int> FirewoodChanged;

        public PooledButtonList Roster => _roster;
        public PooledButtonList Recruits => _recruits;

        protected override void BuildContent(RectTransform root)
        {
            var left = Column(root, "Roster", 0.01f, 0.33f, 0.02f, 0.98f);
            _header = Factory.CreateText(left, "Header", UiTheme.TitleFontSize, TextAnchor.MiddleLeft, 60f);
            Factory.CreateText(left, "RosterTitle", UiTheme.SmallFontSize, TextAnchor.MiddleLeft, 30f).text = "명부 (클릭: 선택 / 파티 편성 토글)";
            _roster = new PooledButtonList(Factory, Factory.CreateVertical(left, "RosterList"));

            var middle = Column(root, "Details", 0.34f, 0.66f, 0.02f, 0.98f);
            _details = Factory.CreateText(middle, "Details", UiTheme.BodyFontSize);
            AddButton(middle, "선술집 (스트레스 치료)", () => TreatClicked?.Invoke());
            AddButton(middle, "해고", () => DismissClicked?.Invoke());
            _message = Factory.CreateText(middle, "Message", UiTheme.SmallFontSize, TextAnchor.MiddleLeft, 60f);

            var right = Column(root, "Provision", 0.67f, 0.99f, 0.02f, 0.98f);
            Factory.CreateText(right, "RecruitTitle", UiTheme.SmallFontSize, TextAnchor.MiddleLeft, 30f).text = "역마차 (모집)";
            _recruits = new PooledButtonList(Factory, Factory.CreateVertical(right, "RecruitList"));
            _supplies = Factory.CreateText(right, "Supplies", UiTheme.BodyFontSize, TextAnchor.MiddleLeft, 90f);
            var foodRow = Factory.CreateHorizontal(right, "FoodRow");
            AddButton(foodRow, "식량 -", () => FoodChanged?.Invoke(-1));
            AddButton(foodRow, "식량 +", () => FoodChanged?.Invoke(1));
            var torchRow = Factory.CreateHorizontal(right, "TorchRow");
            AddButton(torchRow, "횃불 -", () => TorchChanged?.Invoke(-1));
            AddButton(torchRow, "횃불 +", () => TorchChanged?.Invoke(1));
            var woodRow = Factory.CreateHorizontal(right, "FirewoodRow");
            AddButton(woodRow, "장작 -", () => FirewoodChanged?.Invoke(-1));
            AddButton(woodRow, "장작 +", () => FirewoodChanged?.Invoke(1));
            AddButton(right, "▶ 던전 출발", () => EmbarkClicked?.Invoke());
            var saveRow = Factory.CreateHorizontal(right, "SaveRow");
            AddButton(saveRow, "저장", () => SaveClicked?.Invoke());
            AddButton(saveRow, "새 게임", () => NewGameClicked?.Invoke());
        }

        public void SetHeader(string text) => _header.text = text;
        public void SetDetails(string text) => _details.text = text;
        public void SetSupplies(string text) => _supplies.text = text;
        public void SetMessage(string text) => _message.text = text;
    }
}
